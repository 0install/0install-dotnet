// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Runs test methods for <see cref="SatSolver"/>.
/// </summary>
public class SatSolverTest : SolverTest
{
    protected override ISolver BuildSolver(ISelectionCandidateProvider candidateProvider)
        => new SatSolver(candidateProvider);

    [Fact]
    public void PlainDependencyAllowsMixingCpuGroups()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var libUri = new FeedUri("http://example.com/lib.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands = {new() {Name = Command.NameRun, Path = "app"}},
                            Dependencies = {new Dependency {InterfaceUri = libUri}}
                        }
                    }
                },
                new Feed
                {
                    Uri = libUri,
                    Name = "lib",
                    Elements =
                    {
                        new Implementation {ID = "lib32", Version = new("1.0"), Architecture = new(OS.All, Cpu.I686)}
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual[appUri].ID.Should().Be("app64");
        actual[libUri].ID.Should().Be("lib32");
    }

    [Fact]
    public void EnvironmentBindingKeepsCpuGroupsConsistent()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var libUri = new FeedUri("http://example.com/lib.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands = {new() {Name = Command.NameRun, Path = "app"}},
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = libUri,
                                    Bindings = {new EnvironmentBinding {Name = "LIB_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = libUri,
                    Name = "lib",
                    Elements =
                    {
                        new Implementation {ID = "lib32", Version = new("2.0"), Architecture = new(OS.All, Cpu.I686)},
                        new Implementation {ID = "lib64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual[appUri].ID.Should().Be("app64");
        actual[libUri].ID.Should().Be("lib64");
    }

    [Fact]
    public void OverlayBindingKeepsCpuGroupsConsistent()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var libUri = new FeedUri("http://example.com/lib.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands = {new() {Name = Command.NameRun, Path = "app"}},
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = libUri,
                                    Bindings = {new OverlayBinding {MountPoint = "/opt/lib"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = libUri,
                    Name = "lib",
                    Elements =
                    {
                        new Implementation {ID = "lib32", Version = new("2.0"), Architecture = new(OS.All, Cpu.I686)},
                        new Implementation {ID = "lib64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual[appUri].ID.Should().Be("app64");
        actual[libUri].ID.Should().Be("lib64");
    }

    [Fact]
    public void ArchNeutralIntermediateKeepsCpuGroupsConsistent()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var wrapperUri = new FeedUri("http://example.com/wrapper.xml");
        var libUri = new FeedUri("http://example.com/lib.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands = {new() {Name = Command.NameRun, Path = "app"}},
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = wrapperUri,
                                    Bindings = {new EnvironmentBinding {Name = "WRAPPER_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = wrapperUri,
                    Name = "wrapper",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "wrapper",
                            Version = new("1.0"),
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = libUri,
                                    Bindings = {new EnvironmentBinding {Name = "LIB_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = libUri,
                    Name = "lib",
                    Elements =
                    {
                        new Implementation {ID = "lib32", Version = new("2.0"), Architecture = new(OS.All, Cpu.I686)},
                        new Implementation {ID = "lib64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual[appUri].ID.Should().Be("app64");
        actual[wrapperUri].ID.Should().Be("wrapper");
        actual[libUri].ID.Should().Be("lib64");
    }

    [Fact]
    public void ArchNeutralParentKeepsCpuGroupsConsistent()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var libAUri = new FeedUri("http://example.com/lib-a.xml");
        var libBUri = new FeedUri("http://example.com/lib-b.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app",
                            Version = new("1.0"),
                            Commands = {new() {Name = Command.NameRun, Path = "app"}},
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = libAUri,
                                    Bindings = {new EnvironmentBinding {Name = "LIB_A_PATH"}}
                                },
                                new Dependency
                                {
                                    InterfaceUri = libBUri,
                                    Bindings = {new EnvironmentBinding {Name = "LIB_B_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = libAUri,
                    Name = "lib-a",
                    Elements =
                    {
                        new Implementation {ID = "libA64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                },
                new Feed
                {
                    Uri = libBUri,
                    Name = "lib-b",
                    Elements =
                    {
                        new Implementation {ID = "libB32", Version = new("2.0"), Architecture = new(OS.All, Cpu.I686)},
                        new Implementation {ID = "libB64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual[libAUri].ID.Should().Be("libA64");
        actual[libBUri].ID.Should().Be("libB64");
    }

    [Fact]
    public void ArchNeutralParentSharesCpuGroupWithRunner()
    {
        var scriptUri = new FeedUri("http://example.com/script.xml");
        var interpreterUri = new FeedUri("http://example.com/interpreter.xml");
        var libUri = new FeedUri("http://example.com/lib.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = scriptUri,
                    Name = "script",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "script",
                            Version = new("1.0"),
                            Commands =
                            {
                                new()
                                {
                                    Name = Command.NameRun,
                                    Path = "script",
                                    Runner = new Runner {InterfaceUri = interpreterUri}
                                }
                            },
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = libUri,
                                    Bindings = {new EnvironmentBinding {Name = "LIB_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = interpreterUri,
                    Name = "interpreter",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "interpreter64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands = {new() {Name = Command.NameRun, Path = "interpreter"}}
                        }
                    }
                },
                new Feed
                {
                    Uri = libUri,
                    Name = "lib",
                    Elements =
                    {
                        new Implementation {ID = "lib32", Version = new("2.0"), Architecture = new(OS.All, Cpu.I686)},
                        new Implementation {ID = "lib64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                }
            ],
            requirements: new Requirements(scriptUri, Command.NameRun));

        actual[scriptUri].ID.Should().Be("script");
        actual[interpreterUri].ID.Should().Be("interpreter64");
        actual[libUri].ID.Should().Be("lib64");
    }

    [Fact]
    public void RunnerAllowsMixingCpuGroups()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var runnerUri = new FeedUri("http://example.com/runner.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands =
                            {
                                new()
                                {
                                    Name = Command.NameRun,
                                    Path = "app",
                                    Runner = new Runner {InterfaceUri = runnerUri}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = runnerUri,
                    Name = "runner",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "runner32",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.I686),
                            Commands = {new() {Name = Command.NameRun, Path = "runner"}}
                        }
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual[appUri].ID.Should().Be("app64");
        actual[runnerUri].ID.Should().Be("runner32");
    }

    [Fact]
    public void ExecutableBindingsAllowMixingCpuGroups()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var toolVarUri = new FeedUri("http://example.com/tool-var.xml");
        var toolPathUri = new FeedUri("http://example.com/tool-path.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands = {new() {Name = Command.NameRun, Path = "app"}},
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = toolVarUri,
                                    Bindings = {new ExecutableInVar {Name = "TOOL_VAR", Command = Command.NameRun}}
                                },
                                new Dependency
                                {
                                    InterfaceUri = toolPathUri,
                                    Bindings = {new ExecutableInPath {Name = "tool-path", Command = Command.NameRun}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = toolVarUri,
                    Name = "tool-var",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "toolVar32",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.I686),
                            Commands = {new() {Name = Command.NameRun, Path = "tool"}}
                        }
                    }
                },
                new Feed
                {
                    Uri = toolPathUri,
                    Name = "tool-path",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "toolPath32",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.I686),
                            Commands = {new() {Name = Command.NameRun, Path = "tool"}}
                        }
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual[appUri].ID.Should().Be("app64");
        actual[toolVarUri].ID.Should().Be("toolVar32");
        actual[toolPathUri].ID.Should().Be("toolPath32");
    }

    [Fact]
    public void SeparateEnvironmentBoundComponentsCanUseDifferentCpuGroups()
    {
        var rootUri = new FeedUri("http://example.com/root.xml");
        var componentAUri = new FeedUri("http://example.com/component-a.xml");
        var componentALibUri = new FeedUri("http://example.com/component-a-lib.xml");
        var componentBUri = new FeedUri("http://example.com/component-b.xml");
        var componentBLibUri = new FeedUri("http://example.com/component-b-lib.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = rootUri,
                    Name = "root",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "root",
                            Version = new("1.0"),
                            Commands = {new() {Name = Command.NameRun, Path = "root"}},
                            Dependencies =
                            {
                                new Dependency {InterfaceUri = componentAUri},
                                new Dependency {InterfaceUri = componentBUri}
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = componentAUri,
                    Name = "component-a",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "componentA64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = componentALibUri,
                                    Bindings = {new EnvironmentBinding {Name = "A_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = componentALibUri,
                    Name = "component-a-lib",
                    Elements =
                    {
                        new Implementation {ID = "componentALib64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                },
                new Feed
                {
                    Uri = componentBUri,
                    Name = "component-b",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "componentB32",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.I686),
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = componentBLibUri,
                                    Bindings = {new EnvironmentBinding {Name = "B_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = componentBLibUri,
                    Name = "component-b-lib",
                    Elements =
                    {
                        new Implementation {ID = "componentBLib32", Version = new("1.0"), Architecture = new(OS.All, Cpu.I686)}
                    }
                }
            ],
            requirements: new Requirements(rootUri, Command.NameRun));

        actual[componentAUri].ID.Should().Be("componentA64");
        actual[componentALibUri].ID.Should().Be("componentALib64");
        actual[componentBUri].ID.Should().Be("componentB32");
        actual[componentBLibUri].ID.Should().Be("componentBLib32");
    }

    [Fact]
    public void UnselectedRecommendedDependencyDoesNotJoinCpuGroups()
    {
        var rootUri = new FeedUri("http://example.com/root.xml");
        var componentAUri = new FeedUri("http://example.com/component-a.xml");
        var componentBUri = new FeedUri("http://example.com/component-b.xml");
        var sharedUri = new FeedUri("http://example.com/shared.xml");

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = rootUri,
                    Name = "root",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "root",
                            Version = new("1.0"),
                            Commands = {new() {Name = Command.NameRun, Path = "root"}},
                            Dependencies =
                            {
                                new Dependency {InterfaceUri = componentAUri},
                                new Dependency {InterfaceUri = componentBUri}
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = componentAUri,
                    Name = "component-a",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "componentA64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = sharedUri,
                                    Importance = Importance.Recommended,
                                    Bindings = {new EnvironmentBinding {Name = "SHARED_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = componentBUri,
                    Name = "component-b",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "componentB32",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.I686),
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = sharedUri,
                                    Importance = Importance.Recommended,
                                    Bindings = {new EnvironmentBinding {Name = "SHARED_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = sharedUri,
                    Name = "shared",
                    Elements =
                    {
                        new Implementation {ID = "shared64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                }
            ],
            requirements: new Requirements(rootUri, Command.NameRun));

        actual[componentAUri].ID.Should().Be("componentA64");
        actual[componentBUri].ID.Should().Be("componentB32");
        actual.ContainsImplementation(sharedUri).Should().BeFalse();
    }

    [Fact]
    public void DiagnosticsReportCpuMismatchWithinProcess()
    {
        var appUri = new FeedUri("http://example.com/app.xml");
        var libUri = new FeedUri("http://example.com/lib.xml");

        var exception = this.Invoking(x => x.Solve(
            feeds:
            [
                new Feed
                {
                    Uri = appUri,
                    Name = "app",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "app64",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.X64),
                            Commands = {new() {Name = Command.NameRun, Path = "app"}},
                            Dependencies =
                            {
                                new Dependency
                                {
                                    InterfaceUri = libUri,
                                    Bindings = {new EnvironmentBinding {Name = "LIB_PATH"}}
                                }
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = libUri,
                    Name = "lib",
                    Elements =
                    {
                        new Implementation {ID = "lib32", Version = new("1.0"), Architecture = new(OS.All, Cpu.I686)}
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun))).Should().Throw<SolverException>().Which;

        exception.Message.Split('\n').Should().Contain(line => line.Contains("lib32 (1.0): ") && line.Contains(appUri.ToString()));
    }

    [Fact]
    public void DiagnosticsDoNotReportCpuMismatchAcrossProcesses()
    {
        var rootUri = new FeedUri("http://example.com/root.xml");
        var componentAUri = new FeedUri("http://example.com/component-a.xml");
        var componentBUri = new FeedUri("http://example.com/component-b.xml");
        var libUri = new FeedUri("http://example.com/lib.xml");

        var exception = this.Invoking(x => x.Solve(
            feeds:
            [
                new Feed
                {
                    Uri = rootUri,
                    Name = "root",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "root",
                            Version = new("1.0"),
                            Commands = {new() {Name = Command.NameRun, Path = "root"}},
                            Dependencies =
                            {
                                new Dependency {InterfaceUri = componentAUri},
                                new Dependency {InterfaceUri = componentBUri}
                            }
                        }
                    }
                },
                new Feed
                {
                    Uri = componentAUri,
                    Name = "component-a",
                    Elements =
                    {
                        new Implementation {ID = "componentA64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                },
                new Feed
                {
                    Uri = componentBUri,
                    Name = "component-b",
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "componentB32",
                            Version = new("1.0"),
                            Architecture = new(OS.All, Cpu.I686),
                            Dependencies = {new Dependency {InterfaceUri = libUri, Versions = new("2.0..")}}
                        }
                    }
                },
                new Feed
                {
                    Uri = libUri,
                    Name = "lib",
                    Elements =
                    {
                        new Implementation {ID = "lib", Version = new("1.0")}
                    }
                }
            ],
            requirements: new Requirements(rootUri, Command.NameRun))).Should().Throw<SolverException>().Which;

        exception.Message.Should().Contain($"{componentBUri} -> 1.0 (componentB32)");
    }
}
