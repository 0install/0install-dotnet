// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Runs test methods for <see cref="SatSolver"/>.
/// </summary>
public class SatSolverTest : SolverTest
{
    public static new List<object[]> TestCases
        => SolverTest.TestCases
            .Where(x => x[0] is TestCase testCase
                     && testCase.Name is not "do-not-mix-x86-and-x64"
                     and not "mismatched-machine-types")
            .ToList();

    protected override ISolver BuildSolver(ISelectionCandidateProvider candidateProvider)
        => new SatSolver(candidateProvider);

    [Theory]
    [MemberData(nameof(TestCases))]
    public void TestCase(TestCase testCase)
        => AssertTestCase(testCase);

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
                        new Implementation {ID = "lib32", Version = new("1.0"), Architecture = new(OS.All, Cpu.I686)},
                        new Implementation {ID = "lib64", Version = new("1.0"), Architecture = new(OS.All, Cpu.X64)}
                    }
                }
            ],
            requirements: new Requirements(appUri, Command.NameRun));

        actual.GetImplementation(appUri)?.ID.Should().Be("app64");
        actual.GetImplementation(libUri)?.ID.Should().Be("lib64");
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

        actual.GetImplementation(appUri)?.ID.Should().Be("app64");
        actual.GetImplementation(runnerUri)?.ID.Should().Be("runner32");
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

        actual.GetImplementation(appUri)?.ID.Should().Be("app64");
        actual.GetImplementation(toolVarUri)?.ID.Should().Be("toolVar32");
        actual.GetImplementation(toolPathUri)?.ID.Should().Be("toolPath32");
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

        actual.GetImplementation(componentAUri)?.ID.Should().Be("componentA64");
        actual.GetImplementation(componentALibUri)?.ID.Should().Be("componentALib64");
        actual.GetImplementation(componentBUri)?.ID.Should().Be("componentB32");
        actual.GetImplementation(componentBLibUri)?.ID.Should().Be("componentBLib32");
    }
}
