// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Contains test methods for <see cref="EnvironmentBuilder"/>.
/// </summary>
public class EnvironmentBuilderTest : TestWithRedirect
{
    private const string Test1Path = "test1 path", Test2Path = "test2 path";

    [Fact]
    public void Exceptions()
    {
        var executor = new EnvironmentBuilder(Mock.Of<IImplementationStore>());

        executor.Invoking(x => x.Inject(new Selections
                 {
                     InterfaceUri = FeedTest.Test1Uri,
                     Command = Command.NameRun
                 }))
                .Should().Throw<ExecutorException>(because: "Selections with no implementations should be rejected");

        executor.Invoking(x => x.Inject(new Selections
                 {
                     InterfaceUri = FeedTest.Test1Uri,
                     Implementations =
                     {
                         new()
                         {
                             InterfaceUri = FeedTest.Test1Uri,
                             ID = "test",
                             Version = new("1.0")
                         }
                     },
                 }))
                .Should().Throw<ExecutorException>(because: "Selections with no start command should be rejected");
    }

    [Fact]
    public void ExceptionConflictingEnvironmentInsertAndValue()
    {
        var selections = SelectionsTest.CreateTestSelections();
        selections.Implementations[1].Commands[0].Bindings.Add(new EnvironmentBinding { Name = "test", Insert = "test1", Value = "test2" });
        ExpectCommandException(selections);
    }

    [Fact]
    public void ExceptionMissingEnvironmentBindingName()
    {
        var selections = SelectionsTest.CreateTestSelections();
        selections.Implementations[1].Commands[0].Bindings.Add(new EnvironmentBinding {Name = null!});
        ExpectCommandException(selections);
    }

    [Fact]
    public void ExceptionMissingExecutableInVarBindingName()
    {
        var selections = SelectionsTest.CreateTestSelections();
        selections.Implementations[1].Commands[0].Bindings.Add(new ExecutableInVar {Name = null!});
        ExpectCommandException(selections);
    }


    [Fact]
    public void ExceptionMissingExecutableInPathBindingName()
    {
        var selections = SelectionsTest.CreateTestSelections();
        selections.Implementations[1].Commands[0].Bindings.Add(new ExecutableInPath {Name = null!});
        ExpectCommandException(selections);
    }

    [Fact]
    public void ExceptionMultipleWorkingDirs()
    {
        var selections = SelectionsTest.CreateTestSelections();
        selections.Implementations[1].Commands[0].WorkingDir = new WorkingDir();
        ExpectCommandException(selections);
    }

    private static void ExpectCommandException(Selections selections)
    {
        var storeMock = new Mock<IImplementationStore>(MockBehavior.Loose);
        storeMock.Setup(x => x.GetPath(It.IsAny<ManifestDigest>())).Returns("test path");
        var executor = new EnvironmentBuilder(storeMock.Object);
        executor.Invoking(x => x.Inject(selections))
                .Should().Throw<ExecutorException>(because: "Invalid Selections should be rejected");
    }

    private static IImplementationStore GetMockStore(Selections selections)
    {
        var storeMock = new Mock<IImplementationStore>(MockBehavior.Loose);
        storeMock.Setup(x => x.GetPath(selections.Implementations[1].ManifestDigest)).Returns(Test1Path);
        storeMock.Setup(x => x.GetPath(selections.Implementations[2].ManifestDigest)).Returns(Test2Path);
        return storeMock.Object;
    }

    private static void VerifyEnvironment(ProcessStartInfo startInfo, Selections selections)
    {
        startInfo.EnvironmentVariables["TEST1_PATH_SELF"].Should().Be($"default{Path.PathSeparator}{Test1Path}", because: "Should append implementation path");
        startInfo.EnvironmentVariables["TEST1_VALUE"].Should().Be("test1", because: "Should directly set value");
        startInfo.EnvironmentVariables["TEST1_EMPTY"].Should().Be("", because: "Should set empty environment variables");
        startInfo.EnvironmentVariables["TEST2_PATH_SELF"].Should().Be($"{Test2Path}{Path.PathSeparator}default", because: "Should prepend implementation path");
        startInfo.EnvironmentVariables["TEST2_VALUE"].Should().Be("test2", because: "Should directly set value");
        startInfo.EnvironmentVariables["TEST2_PATH_SUB_DEP"].Should().Be($"default{Path.PathSeparator}{Path.Combine(Test2Path, "sub")}", because: "Should append implementation sub-path");
        startInfo.EnvironmentVariables["TEST1_PATH_COMMAND"].Should().Be(Test1Path, because: "Should set implementation path");
        startInfo.EnvironmentVariables["TEST1_PATH_COMMAND_DEP"].Should().Be(Test1Path + Path.PathSeparator + Test1Path, because: "Should set implementation path for command dependency for each reference");
        startInfo.WorkingDirectory.Should().Be(Path.Combine(Test1Path, "bin"), because: "Should set implementation path");

        string execFile = Path.Combine(Test2Path, selections.Implementations[2].Commands[0].Path.ToNativePath()!);
        var execArgs = new[]
        {
            selections.Implementations[2].Commands[0].Arguments[0].ToString(),
            selections.Implementations[1].Commands[1].Runner!.Arguments[0].ToString(),
            Path.Combine(Test1Path, selections.Implementations[1].Commands[1].Path.ToNativePath()!),
            selections.Implementations[1].Commands[1].Arguments[0].ToString()
        };
        if (WindowsUtils.IsWindows)
        {
            startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_FILE_exec-in-var"].Should().Be(execFile);
            startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_ARGS_exec-in-var"].Should().Be(execArgs.JoinEscapeArguments());
            startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_FILE_exec-in-path"].Should().Be(execFile);
            startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_ARGS_exec-in-path"].Should().Be(execArgs.JoinEscapeArguments());
        }
        else
        {
            startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_exec_in_var"].Should().Be(EnvironmentBuilder.ToPosixCommandLine(execArgs.Prepend(execFile)));
            startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_exec_in_path"].Should().Be(EnvironmentBuilder.ToPosixCommandLine(execArgs.Prepend(execFile)));
        }
        startInfo.EnvironmentVariables["PATH"].Should().Be(Locations.GetCacheDirPath("0install.net", false, "injector", "executables", "exec-in-path") + Path.PathSeparator + new ProcessStartInfo().EnvironmentVariables["PATH"]);
    }

    /// <summary>
    /// Ensures <see cref="EnvironmentBuilder.ToStartInfo"/> handles complex <see cref="Selections"/>.
    /// </summary>
    [Fact]
    public void Baseline()
    {
        var selections = SelectionsTest.CreateTestSelections();
        AddDummyImplementation(selections);

        var startInfo = new EnvironmentBuilder(GetMockStore(selections))
                       .Inject(selections)
                       .AddArguments("--custom$arg")
                       .ToStartInfo();
        startInfo.FileName.Should().Be(
            Path.Combine(Test2Path, selections.Implementations[2].Commands[0].Path.ToNativePath()!),
            because: "Should combine runner implementation directory with runner command path");
        startInfo.Arguments.Should().Be(
            new[]
            {
                selections.Implementations[2].Commands[0].Arguments[0].ToString(),
                selections.Implementations[1].Commands[0].Runner!.Arguments[0].ToString(),
                Path.Combine(Test1Path, selections.Implementations[1].Commands[0].Path.ToNativePath()!),
                selections.Implementations[1].Commands[0].Arguments[0].ToString(),
                "--custom$arg"
            }.JoinEscapeArguments(),
            because: "Should combine core and additional runner arguments with application implementation directory, command path and arguments");

        VerifyEnvironment(startInfo, selections);
    }

    private static void AddDummyImplementation(Selections selections)
        => selections.Implementations.Insert(0, new()
        {
            InterfaceUri = new("http://example.com/dummy.xml"),
            ID = "dummy",
            Version = new("1.0")
        });

    /// <summary>
    /// Ensures <see cref="EnvironmentBuilder.ToStartInfo"/> handles complex <see cref="Selections"/>.
    /// </summary>
    [Fact]
    public void TestWrapper()
    {
        var selections = SelectionsTest.CreateTestSelections();
        AddDummyImplementation(selections);

        var startInfo = new EnvironmentBuilder(GetMockStore(selections))
                       .Inject(selections)
                       .AddWrapper("wrapper --wrapper")
                       .AddArguments("--custom")
                       .ToStartInfo();
        startInfo.FileName.Should().Be("wrapper");
        startInfo.Arguments.Should().Be(
            new[]
            {
                "--wrapper",
                Path.Combine(Test2Path, selections.Implementations[2].Commands[0].Path.ToNativePath()!),
                selections.Implementations[2].Commands[0].Arguments[0].ToString(),
                selections.Implementations[1].Commands[0].Runner!.Arguments[0].ToString(),
                Path.Combine(Test1Path, selections.Implementations[1].Commands[0].Path.ToNativePath()!),
                selections.Implementations[1].Commands[0].Arguments[0].ToString(),
                "--custom"
            }.JoinEscapeArguments(),
            because: "Should combine wrapper arguments, runner and application");

        VerifyEnvironment(startInfo, selections);
    }

    /// <summary>
    /// Ensures <see cref="EnvironmentBuilder.ToStartInfo"/> handles complex <see cref="Selections"/> and "main" overrides with relative paths.
    /// </summary>
    [Fact]
    public void MainRelative()
    {
        var selections = SelectionsTest.CreateTestSelections();
        AddDummyImplementation(selections);

        var startInfo = new EnvironmentBuilder(GetMockStore(selections))
                       .Inject(selections, overrideMain: "main")
                       .AddArguments("--custom")
                       .ToStartInfo();
        startInfo.FileName.Should().Be(
            Path.Combine(Test2Path, selections.Implementations[2].Commands[0].Path.ToNativePath()!),
            because: "Should combine runner implementation directory with runner command path");
        startInfo.Arguments.Should().Be(
            new[]
            {
                selections.Implementations[2].Commands[0].Arguments[0].ToString(),
                selections.Implementations[1].Commands[0].Runner!.Arguments[0].ToString(),
                Path.Combine(Test1Path, "dir 1", "main"),
                "--custom"
            }.JoinEscapeArguments(),
            because: "Should combine core and additional runner arguments with application implementation directory, command directory and main binary override");

        VerifyEnvironment(startInfo, selections);
    }

    /// <summary>
    /// Ensures <see cref="EnvironmentBuilder.ToStartInfo"/> handles complex <see cref="Selections"/> and "main" overrides with absolute paths.
    /// </summary>
    [Fact]
    public void MainAbsolute()
    {
        var selections = SelectionsTest.CreateTestSelections();
        AddDummyImplementation(selections);

        var startInfo = new EnvironmentBuilder(GetMockStore(selections))
                       .Inject(selections, overrideMain: "/main")
                       .AddArguments("--custom")
                       .ToStartInfo();
        startInfo.FileName.Should().Be(
            Path.Combine(Test2Path, selections.Implementations[2].Commands[0].Path.ToNativePath()!),
            because: "Should combine runner implementation directory with runner command path");
        startInfo.Arguments.Should().Be(
            new[]
            {
                selections.Implementations[2].Commands[0].Arguments[0].ToString(),
                selections.Implementations[1].Commands[0].Runner!.Arguments[0].ToString(),
                Path.Combine(Test1Path, "main"),
                "--custom"
            }.JoinEscapeArguments(),
            because: "Should combine core and additional runner arguments with application implementation directory and main binary override");

        VerifyEnvironment(startInfo, selections);
    }

    /// <summary>
    /// Ensures <see cref="EnvironmentBuilder.ToStartInfo"/> handles <see cref="Selections"/> with <see cref="Command.Path"/>s that are empty.
    /// </summary>
    [Fact]
    public void PathlessCommand()
    {
        var selections = SelectionsTest.CreateTestSelections();
        AddDummyImplementation(selections);
        selections.Implementations[1].Commands[0].Path = null;

        var startInfo = new EnvironmentBuilder(GetMockStore(selections))
                       .Inject(selections)
                       .AddArguments("--custom")
                       .ToStartInfo();
        startInfo.FileName.Should().Be(
            Path.Combine(Test2Path, selections.Implementations[2].Commands[0].Path.ToNativePath()!));
        startInfo.Arguments.Should().Be(
            new[]
            {
                selections.Implementations[2].Commands[0].Arguments[0].ToString(),
                selections.Implementations[1].Commands[0].Runner!.Arguments[0].ToString(),
                selections.Implementations[1].Commands[0].Arguments[0].ToString(),
                "--custom"
            }.JoinEscapeArguments());

        VerifyEnvironment(startInfo, selections);
    }

    /// <summary>
    /// Ensures <see cref="EnvironmentBuilder.ToStartInfo"/> handles <see cref="Selections"/> with <see cref="ForEachArgs"/>.
    /// </summary>
    [Fact]
    public void ForEachArgs()
    {
        var selections = SelectionsTest.CreateTestSelections();
        AddDummyImplementation(selections);

        selections.Implementations[1].Commands[0].Arguments.Add(new ForEachArgs
        {
            ItemFrom = "SPLIT_ARG",
            Arguments = {"pre1 $item post1", "pre2 $item post2"}
        });
        selections.Implementations[2].Bindings.Add(new EnvironmentBinding {Name = "SPLIT_ARG", Value = $"split1{Path.PathSeparator}split2" });

        var startInfo = new EnvironmentBuilder(GetMockStore(selections))
                       .Inject(selections)
                       .ToStartInfo();
        startInfo.FileName.Should().Be(
            Path.Combine(Test2Path, selections.Implementations[2].Commands[0].Path.ToNativePath()!),
            because: "Should combine runner implementation directory with runner command path");
        startInfo.Arguments.Should().Be(
            new[]
            {
                selections.Implementations[2].Commands[0].Arguments[0].ToString(),
                selections.Implementations[1].Commands[0].Runner!.Arguments[0].ToString(),
                Path.Combine(Test1Path, selections.Implementations[1].Commands[0].Path.ToNativePath()!),
                selections.Implementations[1].Commands[0].Arguments[0].ToString(),
                "pre1 split1 post1",
                "pre2 split1 post2",
                "pre1 split2 post1",
                "pre2 split2 post2"
            }.JoinEscapeArguments(),
            because: "Should combine core and additional runner arguments with application implementation directory and command path");

        VerifyEnvironment(startInfo, selections);
    }

    [Fact]
    public void PosixCommandLine()
    {
        EnvironmentBuilder.ToPosixCommandLine(["/path/to/app", "--arg", "a,b", "1.0+x@y:z%"])
                          .Should().Be("/path/to/app --arg a,b 1.0+x@y:z%", because: "Should not quote when not needed, for compatibility with older scripts");
        EnvironmentBuilder.ToPosixCommandLine(["/home/müller/app", "田中", "\U0001F600"])
                          .Should().Be("/home/müller/app 田中 \U0001F600", because: "Non-ASCII characters have no special meaning for a shell");
        EnvironmentBuilder.ToPosixCommandLine(["/my dir/app", ""])
                          .Should().Be("'/my dir/app' ''");
        EnvironmentBuilder.ToPosixCommandLine(["a b", "a　b", "a\u0085b"])
                          .Should().Be("'a b' 'a　b' 'a\u0085b'", because: "Non-ASCII whitespace should stay quoted");
        EnvironmentBuilder.ToPosixCommandLine(["it's", @"back\slash", "$HOME", "\"quoted\"", "a=b", "*", "#", "~", "`x`", "line\nbreak"])
                          .Should().Be("""'it'\''s' 'back\slash' '$HOME' '"quoted"' 'a=b' '*' '#' '~' '`x`' """ + "'line\nbreak'");
    }

    private const string RunEnvName = "my-tool";
    private const string RunEnvSpecialChars = """a'b "c" \d $HOME $(false) `false` * #e ~ ;f""";

    /// <summary>
    /// Launches a run-environment executable for an implementation located in a path that needs quoting.
    /// </summary>
    /// <returns>The arguments received by the implementation's executable.</returns>
    /// <param name="tempDir">The directory to place the implementation in.</param>
    /// <param name="binding">The binding that provides the run-environment executable.</param>
    /// <param name="shellCommand">The command to launch the executable with. Uses sh, since some implementations of it (e.g. dash) remove variables with names that are not valid in a shell from the environment.</param>
    private static string[] RunRunEnvExecutable(string tempDir, Binding binding, string shellCommand)
    {
        string implementationPath = Path.Combine(tempDir, "it's my dir");
        Directory.CreateDirectory(implementationPath);
        string appPath = Path.Combine(implementationPath, "app");
        File.WriteAllText(appPath, "#!/bin/sh\nprintf '%s\\n' \"$@\"\n");
        FileUtils.SetExecutable(appPath, true);

        var selections = new Selections
        {
            InterfaceUri = FeedTest.Test1Uri,
            Command = Command.NameRun,
            Implementations =
            {
                new()
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    ID = "test",
                    Version = new("1.0"),
                    ManifestDigest = new(Sha256New: "123"),
                    Bindings = {new EnvironmentBinding {Name = "TEST_SPECIAL", Value = RunEnvSpecialChars}, binding},
                    Commands = {new() {Name = Command.NameRun, Path = "app", Arguments = {"arg with space", "", "$TEST_SPECIAL"}}}
                }
            }
        };
        var storeMock = new Mock<IImplementationStore>(MockBehavior.Loose);
        storeMock.Setup(x => x.GetPath(It.IsAny<ManifestDigest>())).Returns(implementationPath);

        var startInfo = new EnvironmentBuilder(storeMock.Object).Inject(selections).ToStartInfo();
        startInfo.FileName = "sh";
        startInfo.Arguments = new[] {"-c", shellCommand}.JoinEscapeArguments();
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;

        using var process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0);

        return output.TrimEnd('\n').Split('\n');
    }

    private static string[] ExpectedRunEnvArgs => ["arg with space", "", RunEnvSpecialChars, "extra arg"];

    private static string GetRunEnvPath(string name)
        => Path.Combine(Locations.GetCacheDirPath("0install.net", false, "injector", "executables", name), name);

    [Fact]
    public void RunEnvExecutableInPath()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests the run-environment script used on Unix");

        using var tempDir = new TemporaryDirectory("0install-test-runenv");
        RunRunEnvExecutable(tempDir, new ExecutableInPath {Name = RunEnvName}, $"{RunEnvName} 'extra arg'")
           .Should().Equal(ExpectedRunEnvArgs);
    }

    [Fact]
    public void RunEnvExecutableInPathSpecialName()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests the run-environment script used on Unix");

        using var tempDir = new TemporaryDirectory("0install-test-runenv");
        RunRunEnvExecutable(tempDir, new ExecutableInPath {Name = "my töol's"}, "\"my töol's\" 'extra arg'")
           .Should().Equal(ExpectedRunEnvArgs);
    }

    [Fact]
    public void RunEnvVarName()
    {
        EnvironmentBuilder.GetRunEnvVarName("my_Tool2").Should().Be("ZEROINSTALL_RUNENV_my_Tool2");
        EnvironmentBuilder.GetRunEnvVarName("my-tool").Should().Be("ZEROINSTALL_RUNENV_my_tool");
        EnvironmentBuilder.GetRunEnvVarName("my töol's").Should().Be("ZEROINSTALL_RUNENV_my_t__ol_s", because: "Should replace each UTF-8 byte individually");
    }

    [Fact]
    public void RunEnvExecutableInVar()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests the run-environment script used on Unix");

        using var tempDir = new TemporaryDirectory("0install-test-runenv");
        RunRunEnvExecutable(tempDir, new ExecutableInVar {Name = "MY_TOOL"}, "\"$MY_TOOL\" 'extra arg'")
           .Should().Equal(ExpectedRunEnvArgs);
    }

    /// <summary>
    /// Ensures scripts left behind by older versions are replaced when deploying but not modified while they might be running.
    /// </summary>
    [Fact]
    public void RunEnvReplacesOlderScripts()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests the run-environment script used on Unix");

        string[] oldScript = ["#!/bin/bash", "env_var_name=ZEROINSTALL_RUNENV_$(basename $0)", "${!env_var_name} \"$@\""];
        string templatePath = Path.Combine(Locations.GetCacheDirPath("0install.net", false, "injector", "executables"), "runenv.sh.template");
        File.WriteAllLines(templatePath, oldScript);
        string otherPath = GetRunEnvPath("other-tool");
        FileUtils.CreateHardlink(GetRunEnvPath(RunEnvName), templatePath);
        FileUtils.CreateHardlink(otherPath, templatePath);

        using var tempDir = new TemporaryDirectory("0install-test-runenv");
        RunRunEnvExecutable(tempDir, new ExecutableInPath {Name = RunEnvName}, $"{RunEnvName} 'extra arg'")
           .Should().Equal(ExpectedRunEnvArgs);

        File.ReadAllLines(otherPath).Should().Equal(oldScript, because: "Scripts not being deployed may be in use and must not be overwritten in-place");
    }
}
