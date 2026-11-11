// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Text;
using AwesomeAssertions.Execution;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Contains test methods for <see cref="Materializer"/>.
/// </summary>
public sealed class MaterializerTest : IDisposable
{
    private const string Special = """a'b "c" \d $HOME $(false) `false` * #e ~ ;f ‘g’""";

    private static readonly FeedUri
        AppUri = new("http://example.com/app.xml"),
        RunnerUri = new("http://example.com/runner.xml"),
        ToolUri = new("http://example.com/tool.xml");

    private readonly TemporaryDirectory _tempDir = new("0install-test-materializer");
    private readonly string _storeDir, _targetDir;
    private readonly IImplementationStore _store;

    public MaterializerTest()
    {
        _storeDir = Path.Combine(_tempDir, "store");
        _targetDir = Path.Combine(_tempDir, "it's a $(target) `dir`");
        Directory.CreateDirectory(_storeDir);

        var storeMock = new Mock<IImplementationStore>();
        storeMock.Setup(x => x.GetPath(It.IsAny<ManifestDigest>()))
                 .Returns<ManifestDigest>(digest => Path.Combine(_storeDir, digest.Best!) is var path && Directory.Exists(path) ? path : null);
        _store = storeMock.Object;
    }

    public void Dispose() => _tempDir.Dispose();

    private Materializer Materializer(OS os, bool useLinks = false, bool clean = false) => new(_store, new SilentTaskHandler()) {TargetOS = os, UseLinks = useLinks, Clean = clean};

    #region POSIX
    [Fact]
    public void RunsWithBindingsRunnerAndExecutables()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "POSIX launcher scripts can only be run on Unix");

        var selections = CreateSelections();
        string launcher = Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");

        launcher.Should().Be(Path.Combine(_targetDir, "my app"));
        VerifyOutput(Run(launcher, "user arg", ""), _targetDir, selections);
        File.Exists(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir, "my-tool")).Should().BeTrue();
    }

    [Fact]
    public void UsesPosixPathsAndSeparators()
    {
        var selections = CreateSelections();
        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");

        string appDir = "\"${_zeroinstall_dir}\"/" + selections[AppUri].ID;
        string runnerDir = "\"${_zeroinstall_dir}\"/" + selections[RunnerUri].ID;
        string toolDir = "\"${_zeroinstall_dir}\"/" + selections[ToolUri].ID;
        File.ReadAllText(Path.Combine(_targetDir, "my app")).Should()
            .NotContain("\r").And
            .Contain("CDPATH= cd --", because: "an inherited CDPATH must not affect locating the launcher's directory").And
            .NotContain("dirname", because: "parameter expansion avoids depending on external utilities").And
            .Contain($"export LIBPATH={appDir}/lib:\"${{LIBPATH}}\"").And
            .Contain("export PATH=\"${_zeroinstall_dir}\"/bin\"${PATH:+:$PATH}\"").And
            .Contain($"cd {appDir}/sub || exit").And
            .Contain($"set -- \"$@\" {runnerDir}/run.sh").And
            .Contain($"set -- \"$@\" {appDir}/app.sh").And
            .Contain("IFS=:").And
            .Contain($"set -- \"$@\" {toolDir}/tool.sh", because: "command-lines for executables are expanded by the main launcher, like Executor does").And
            .Contain("export ZEROINSTALL_RUNENV_my_tool=\"$_zeroinstall_cmd\"");
        File.ReadAllText(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir, "my-tool")).Should()
            .Contain("eval \"exec $ZEROINSTALL_RUNENV_my_tool \\\"\\$@\\\"\"");
    }

    [Fact]
    public void RejectsMultiCharacterSeparatorsInPosixShells()
    {
        var selections = CreateSelections(withTool: false);
        ((ForEachArgs)selections[AppUri].Commands[0].Arguments[2]).Separator = "::";

        Materializer(OS.Linux).Invoking(x => x.Materialize(selections, _targetDir, "my app"))
                              .Should().Throw<ExecutorException>(because: "IFS would split at each character on its own");
        Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");
    }

    [Fact]
    public void RejectsApostropheInDefaultValueInPosixShells()
    {
        var selections = CreateSelections(withTool: false);
        selections[AppUri].Commands[0].Arguments.Add("${UNSET_VAR:-it's}");

        Materializer(OS.Linux).Invoking(x => x.Materialize(selections, _targetDir, "my app"))
                              .Should().Throw<ExecutorException>();
    }

    [Fact]
    public void ExpandsVariablesInDefaultValues()
    {
        var selections = CreateSelections(withTool: false);
        selections[AppUri].Commands[0].Arguments.Add("${UNSET_VAR:-pre$OTHER_VAR}");

        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");
        File.ReadAllText(Path.Combine(_targetDir, "my app")).Should()
            .Contain("\"${UNSET_VAR:-pre${OTHER_VAR}}\"", because: "Executor expands short-style variables in default values");

        Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");
        File.ReadAllText(Path.Combine(_targetDir, "my app.ps1")).Should()
            .Contain("else { ('pre' + $env:OTHER_VAR) }");
    }

    [Fact]
    public void MarksScriptsAsExecutable()
    {
        var selections = CreateSelections();
        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");
        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app"); // Replaces existing scripts

        ImplFileUtils.IsExecutable(Path.Combine(_targetDir, "my app")).Should().BeTrue();
        ImplFileUtils.IsExecutable(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir, "my-tool")).Should().BeTrue();
        ImplFileUtils.IsExecutable(Path.Combine(_targetDir, selections[AppUri].ID, "app.sh")).Should().BeTrue(because: "executable bits of copied files should be preserved");
        ImplFileUtils.IsExecutable(Path.Combine(_targetDir, selections[AppUri].ID, "sub", "readme.txt")).Should().BeFalse();
    }

    [Fact]
    public void IsRelocatable()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "POSIX launcher scripts can only be run on Unix");

        var selections = CreateSelections();
        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");

        string movedDir = Path.Combine(_tempDir, "moved");
        Directory.Move(_targetDir, movedDir);
        VerifyOutput(Run(Path.Combine(movedDir, "my app"), "user arg", ""), movedDir, selections);
    }

#if NET
    [Fact]
    public void FollowsSymlinkToLauncher()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "POSIX launcher scripts can only be run on Unix");

        var selections = CreateSelections();
        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");

        string linkDir = Path.Combine(_tempDir, "links");
        Directory.CreateDirectory(linkDir);
        string linkPath = Path.Combine(linkDir, "app-link");
        File.CreateSymbolicLink(linkPath, Path.Combine("..", Path.GetFileName(_targetDir), "my app"));
        VerifyOutput(Run(linkPath, "user arg", ""), _targetDir, selections);
    }
#endif

    [Fact]
    public void UpdateKeepsStaleContentWithoutClean()
    {
        var oldSelections = CreateSelections();
        Materializer(OS.Linux).Materialize(oldSelections, _targetDir, "my app");

        var newSelections = CreateSelections(runnerVersion: "2", withTool: false);
        Materializer(OS.Linux).Materialize(newSelections, _targetDir, "my app");

        Directory.Exists(Path.Combine(_targetDir, oldSelections[RunnerUri].ID)).Should().BeTrue();
        Directory.Exists(Path.Combine(_targetDir, oldSelections[ToolUri].ID)).Should().BeTrue();
        File.Exists(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir, "my-tool")).Should().BeTrue();
        Directory.Exists(Path.Combine(_targetDir, newSelections[RunnerUri].ID)).Should().BeTrue();

        if (UnixUtils.IsUnix)
            Run(Path.Combine(_targetDir, "my app")).Should().Contain("runner=runner arg v2");
    }

    [Fact]
    public void UpdateRemovesStaleImplementations()
    {
        var oldSelections = CreateSelections();
        Materializer(OS.Linux).Materialize(oldSelections, _targetDir, "my app");
        File.WriteAllText(Path.Combine(_targetDir, "user file.txt"), "keep me");

        var newSelections = CreateSelections(runnerVersion: "2", withTool: false);
        Materializer(OS.Linux, clean: true).Materialize(newSelections, _targetDir, "my app");

        Directory.Exists(Path.Combine(_targetDir, oldSelections[RunnerUri].ID)).Should().BeFalse(because: "old runner is no longer selected");
        Directory.Exists(Path.Combine(_targetDir, oldSelections[ToolUri].ID)).Should().BeFalse(because: "tool is no longer selected");
        Directory.Exists(Path.Combine(_targetDir, newSelections[RunnerUri].ID)).Should().BeTrue();
        Directory.Exists(Path.Combine(_targetDir, newSelections[AppUri].ID)).Should().BeTrue();
        Directory.Exists(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir)).Should().BeFalse(because: "no executable bindings are left");
        File.Exists(Path.Combine(_targetDir, "user file.txt")).Should().BeTrue(because: "files not created by the materializer must be kept");

        if (UnixUtils.IsUnix)
            Run(Path.Combine(_targetDir, "my app")).Should().Contain("runner=runner arg v2");
    }

    [Fact]
    public void UpdateKeepsDirectoriesNotNamedAfterDigest()
    {
        var oldSelections = CreateSelections();
        Materializer(OS.Linux).Materialize(oldSelections, _targetDir, "my app");
        string backupDir = Path.Combine(_targetDir, oldSelections[RunnerUri].ID + "-backup");
        Directory.Move(Path.Combine(_targetDir, oldSelections[RunnerUri].ID), backupDir);

        Materializer(OS.Linux, clean: true).Materialize(CreateSelections(runnerVersion: "2"), _targetDir, "my app");

        File.Exists(Path.Combine(backupDir, "run.sh")).Should().BeTrue(because: "directories with a digest prefix but a different name were not created by the materializer");
    }

    [Fact]
    public void RemovesUnneededFilesInBinDir()
    {
        string binDir = Path.Combine(_targetDir, LauncherScriptBuilder.BinDir);
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "other-file"), "not generated");

        Materializer(OS.Windows).Materialize(CreateSelections(), _targetDir, "my app");
        Materializer(OS.Linux).Materialize(CreateSelections(), _targetDir, "my app");
        File.Exists(Path.Combine(binDir, "my-tool.exe")).Should().BeTrue(because: "nothing is removed without clean");
        File.Exists(Path.Combine(binDir, "other-file")).Should().BeTrue(because: "nothing is removed without clean");

        Materializer(OS.Linux, clean: true).Materialize(CreateSelections(), _targetDir, "my app");

        File.Exists(Path.Combine(binDir, "my-tool")).Should().BeTrue();
        File.Exists(Path.Combine(binDir, "my-tool.exe")).Should().BeFalse(because: "executables generated for a different operating system should be removed");
        File.Exists(Path.Combine(binDir, "other-file")).Should().BeFalse(because: "the directory is reserved for generated executables");

        Materializer(OS.Linux, clean: true).Materialize(CreateSelections(withTool: false), _targetDir, "my app");

        Directory.Exists(binDir).Should().BeFalse(because: "no executable bindings are left");
    }

    [Fact]
    public void LinksImplementations()
    {
        var selections = CreateSelections();
        Materializer(OS.Linux, useLinks: true).Materialize(selections, _targetDir, "my app");
        Materializer(OS.Linux, useLinks: true).Materialize(selections, _targetDir, "my app"); // Replaces existing links

        foreach (var implementation in selections.Implementations)
            new DirectoryInfo(Path.Combine(_targetDir, implementation.ID)).IsDirectoryLink().Should().BeTrue();
        File.Exists(Path.Combine(_targetDir, selections[AppUri].ID, "app.sh")).Should().BeTrue();

        if (UnixUtils.IsUnix)
            Run(Path.Combine(_targetDir, "my app")).Should().Contain("runner=runner arg v1").And.Contain("tool=tool arg");
    }

    [Fact]
    public void UpdateRemovesStaleLinksButKeepsTheirTargets()
    {
        var oldSelections = CreateSelections();
        Materializer(OS.Linux, useLinks: true).Materialize(oldSelections, _targetDir, "my app");

        var newSelections = CreateSelections(runnerVersion: "2", withTool: false);
        Materializer(OS.Linux, useLinks: true, clean: true).Materialize(newSelections, _targetDir, "my app");

        Directory.Exists(Path.Combine(_targetDir, oldSelections[RunnerUri].ID)).Should().BeFalse(because: "old runner is no longer selected");
        Directory.Exists(Path.Combine(_targetDir, oldSelections[ToolUri].ID)).Should().BeFalse(because: "tool is no longer selected");
        Directory.Exists(Path.Combine(_targetDir, newSelections[RunnerUri].ID)).Should().BeTrue();
        VerifyStoreIntact(oldSelections);
    }

    [Fact]
    public void SwitchesBetweenCopiesAndLinks()
    {
        var selections = CreateSelections();
        string appDir = Path.Combine(_targetDir, selections[AppUri].ID);

        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");
        new DirectoryInfo(appDir).IsDirectoryLink().Should().BeFalse();

        Materializer(OS.Linux, useLinks: true).Materialize(selections, _targetDir, "my app");
        new DirectoryInfo(appDir).IsDirectoryLink().Should().BeFalse(because: "existing copies are kept without clean");

        Materializer(OS.Linux, useLinks: true, clean: true).Materialize(selections, _targetDir, "my app");
        new DirectoryInfo(appDir).IsDirectoryLink().Should().BeTrue();

        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");
        new DirectoryInfo(appDir).IsDirectoryLink().Should().BeTrue(because: "existing links are kept without clean");

        Materializer(OS.Linux, clean: true).Materialize(selections, _targetDir, "my app");
        new DirectoryInfo(appDir).IsDirectoryLink().Should().BeFalse();
        File.Exists(Path.Combine(appDir, "app.sh")).Should().BeTrue();

        // Removing the copies must not affect the implementations the links used to point to
        Directory.Delete(_targetDir, recursive: true);
        VerifyStoreIntact(selections);
    }

    private void VerifyStoreIntact(Selections selections)
    {
        foreach (var implementation in selections.Implementations)
        {
            string path = Path.Combine(_storeDir, implementation.ID);
            Manifest.Load(Path.Combine(path, Manifest.ManifestFile), ManifestFormat.Sha256New);
            var builder = new ManifestBuilder(ManifestFormat.Sha256New);
            new SilentTaskHandler().RunTask(new ReadDirectory(path, builder));
            builder.Manifest.CalculateDigest().Should().Be(implementation.ID, because: "implementations in the store must not be modified through links");
        }
    }

    [Fact]
    public void RejectsTargetInsideLocalImplementation()
    {
        var selections = CreateSelections(withTool: false);
        string localDir = Path.Combine(_tempDir, "local");
        Directory.CreateDirectory(localDir);
        File.WriteAllText(Path.Combine(localDir, "app.sh"), "#!/bin/sh\n");
        selections[AppUri].LocalPath = localDir;

        Materializer(OS.Linux).Invoking(x => x.Materialize(selections, Path.Combine(localDir, "dist"), "my app"))
                              .Should().Throw<IOException>();
        Materializer(OS.Linux).Invoking(x => x.Materialize(selections, localDir, "my app"))
                              .Should().Throw<IOException>();
        Directory.Exists(Path.Combine(localDir, "dist")).Should().BeFalse();

        Materializer(OS.Linux).Materialize(selections, localDir + "-dist", "my app"); // Sibling with common prefix
    }

    [Fact]
    public void SkipsNativePackages()
    {
        var selections = CreateSelections();
        var package = new ImplementationSelection
        {
            InterfaceUri = RunnerUri,
            ID = "package:deb:runner:1.0:x86_64",
            Version = new("1.0"),
            Bindings = {new EnvironmentBinding {Name = "RUNNER_HOME", Insert = "", Mode = EnvironmentMode.Replace}},
            Commands = {new() {Name = Command.NameRun, Path = "/usr/bin/runner"}}
        };
        selections.Implementations.Remove(selections[RunnerUri]);
        selections.Implementations.Add(package);

        Materializer(OS.Linux).Materialize(selections, _targetDir, "my app");

        Directory.Exists(Path.Combine(_targetDir, selections[AppUri].ID)).Should().BeTrue();
        Directory.GetDirectories(_targetDir).Should().NotContain(x => Path.GetFileName(x).StartsWith("package:"));
        File.ReadAllText(Path.Combine(_targetDir, "my app")).Should()
            .Contain("set -- \"$@\" /usr/bin/runner", because: "packages are run from the system, with a POSIX path even when materializing on Windows").And
            .NotContain("RUNNER_HOME", because: "bindings for packages are skipped, like in the Executor");
    }
    #endregion

    #region Windows
    [Fact]
    public void RunsWithBindingsRunnerAndExecutablesOnWindows()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "PowerShell launcher scripts can only be run on Windows");

        var selections = CreateWindowsSelections();
        string launcher = Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");

        launcher.Should().Be(Path.Combine(_targetDir, "my app.cmd"));
        VerifyWindowsOutput(RunPowerShell(Path.Combine(_targetDir, "my app.ps1"), "user arg", Special, ""), _targetDir, selections);
        File.Exists(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir, "my-tool.exe")).Should().BeTrue();
    }

    [Fact]
    public void RunsViaCmdWrapperOnWindows()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "PowerShell launcher scripts can only be run on Windows");

        var selections = CreateWindowsSelections();
        string launcher = Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");

        VerifyWindowsOutput(Run(launcher, "user arg", Special, ""), _targetDir, selections);
    }

    [Fact]
    public void CmdWrapperContainsNoFileNames()
    {
        string launcher = Materializer(OS.Windows).Materialize(CreateSelections(), _targetDir, "my app ‘ä’");

        File.ReadAllBytes(launcher).Should().OnlyContain(b => b < 0x80, because: "cmd.exe does not read batch files as UTF-8");
        File.ReadAllText(launcher).Should().Contain("\"%~dpn0.ps1\" %*");
    }

    [Fact]
    public void IsRelocatableOnWindows()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "PowerShell launcher scripts can only be run on Windows");

        var selections = CreateWindowsSelections();
        Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");

        string movedDir = Path.Combine(_tempDir, "moved");
        Directory.Move(_targetDir, movedDir);
        VerifyWindowsOutput(RunPowerShell(Path.Combine(movedDir, "my app.ps1"), "user arg", Special, ""), movedDir, selections);
    }

    [Fact]
    public void UsesWindowsPathsAndSeparators()
    {
        var selections = CreateSelections();
        Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");
        string launcher = Path.Combine(_targetDir, "my app.ps1");

        string appDir = $@"$_zeroinstall_dir + '\{selections[AppUri].ID}";
        string runnerDir = $@"$_zeroinstall_dir + '\{selections[RunnerUri].ID}";
        string toolDir = $@"$_zeroinstall_dir + '\{selections[ToolUri].ID}";
        File.ReadAllBytes(launcher).Take(3).Should().Equal([0xEF, 0xBB, 0xBF], because: "Windows PowerShell needs a BOM to detect UTF-8");
        File.ReadAllText(launcher).Should()
            .NotMatchRegex("[^\r]\n", because: "PowerShell scripts on Windows should use CRLF line endings").And
            .Contain($@"$env:LIBPATH = ({appDir}\lib;') + $env:LIBPATH").And
            .Contain(@"$env:Path = ($_zeroinstall_dir + '\bin;') + $env:Path").And
            .Contain($@"$_zeroinstall_start.WorkingDirectory = ({appDir}\sub')").And
            .Contain($@"({runnerDir}\run.sh')").And
            .Contain($@"({appDir}\app.sh')").And
            .Contain($@"[Environment]::SetEnvironmentVariable('ZEROINSTALL_RUNENV_FILE_my-tool', $_zeroinstall_cmd[0])").And
            .Contain($@"$_zeroinstall_cmd = @(({toolDir}\tool.sh'))").And
            .Contain("-csplit ';'");
    }

    [Fact]
    public void UpdateReplacesExecutablesOnWindows()
    {
        Materializer(OS.Windows).Materialize(CreateSelections(), _targetDir, "my app");
        File.Exists(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir, "my-tool.exe")).Should().BeTrue();

        Materializer(OS.Windows, clean: true).Materialize(CreateSelections(withTool: false), _targetDir, "my app");
        Directory.Exists(Path.Combine(_targetDir, LauncherScriptBuilder.BinDir)).Should().BeFalse(because: "no executable bindings are left");
    }

    [Fact]
    public void UsesWindowsPathsForNativePackages()
    {
        var selections = CreateSelections();
        var package = new ImplementationSelection
        {
            InterfaceUri = RunnerUri,
            ID = "package:windows:runner:1.0",
            Version = new("1.0"),
            Commands = {new() {Name = Command.NameRun, Path = "C:/Program Files/Runner/runner.exe"}}
        };
        selections.Implementations.Remove(selections[RunnerUri]);
        selections.Implementations.Add(package);

        Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");

        File.ReadAllText(Path.Combine(_targetDir, "my app.ps1")).Should().Contain(@"'C:\Program Files\Runner\runner.exe'");
    }

    [Fact]
    public void EmptySeparatorDoesNotSplitOnWindows()
    {
        var selections = CreateSelections(withTool: false);
        ((ForEachArgs)selections[AppUri].Commands[0].Arguments[2]).Separator = "";

        Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");

        File.ReadAllText(Path.Combine(_targetDir, "my app.ps1")).Should()
            .Contain("foreach ($_zeroinstall_item in @($env:ITEMS))").And
            .NotContain("-csplit", because: "-csplit '' would split into single characters");
    }

    [Fact]
    public void SupportsVariableNamesInvalidInPosixShells()
    {
        var selections = CreateSelections(withTool: false);
        selections[AppUri].Bindings.Add(new EnvironmentBinding {Name = "my-var", Value = "x", Mode = EnvironmentMode.Replace});
        selections[AppUri].Commands[0].Arguments.Add("${my-var:-default}");

        Materializer(OS.Windows).Materialize(selections, _targetDir, "my app");

        File.ReadAllText(Path.Combine(_targetDir, "my app.ps1")).Should()
            .Contain("[Environment]::SetEnvironmentVariable('my-var', 'x')").And
            .Contain("$(if ([Environment]::GetEnvironmentVariable('my-var')) { [Environment]::GetEnvironmentVariable('my-var') } else { 'default' })");
    }
    #endregion

    /// <summary>
    /// Creates selections for an app that uses a runner, environment bindings, a working directory, <see cref="ForEachArgs"/> and an <see cref="ExecutableInPath"/> binding.
    /// </summary>
    private Selections CreateSelections(string runnerVersion = "1", bool withTool = true)
    {
        var app = CreateImplementation(AppUri, new()
        {
            ["app.sh"] = """
                #!/bin/sh
                printf 'APP_HOME=%s\n' "$APP_HOME"
                printf 'LIBPATH=%s\n' "$LIBPATH"
                printf 'SPECIAL=%s\n' "$SPECIAL"
                printf 'PWD=%s\n' "$(pwd -P)"
                if command -v my-tool >/dev/null; then my-tool "tool arg"; fi
                for arg in "$@"; do printf 'arg=%s\n' "$arg"; done
                """,
            ["sub/readme.txt"] = "working directory"
        });
        app.Bindings.Add(new EnvironmentBinding {Name = "APP_HOME", Insert = "", Mode = EnvironmentMode.Replace});
        app.Bindings.Add(new EnvironmentBinding {Name = "LIBPATH", Insert = "lib", Mode = EnvironmentMode.Prepend, Default = "default"});
        app.Bindings.Add(new EnvironmentBinding {Name = "SPECIAL", Value = Special, Mode = EnvironmentMode.Replace});
        app.Bindings.Add(new EnvironmentBinding {Name = "ITEMS", Value = "x:y y", Mode = EnvironmentMode.Replace});
        app.Commands.Add(new()
        {
            Name = Command.NameRun,
            Path = "app.sh",
            Arguments = {"--fixed", "$SPECIAL", new ForEachArgs {ItemFrom = "ITEMS", Arguments = {"-i", "${item}"}}},
            Runner = new() {InterfaceUri = RunnerUri, Arguments = {$"runner arg v{runnerVersion}"}},
            WorkingDir = new() {Source = "sub"}
        });

        var runner = CreateImplementation(RunnerUri, new()
        {
            ["run.sh"] = $"""
                #!/bin/sh
                # version {runnerVersion}
                printf 'runner=%s\n' "$1"
                shift
                exec "$@"
                """
        });
        runner.Commands.Add(new() {Name = Command.NameRun, Path = "run.sh"});

        var selections = new Selections
        {
            InterfaceUri = AppUri,
            Command = Command.NameRun,
            Implementations = {app, runner}
        };

        if (withTool)
        {
            var tool = CreateImplementation(ToolUri, new()
            {
                ["tool.sh"] = """
                    #!/bin/sh
                    for arg in "$@"; do printf 'tool=%s\n' "$arg"; done
                    """
            });
            tool.Commands.Add(new() {Name = Command.NameRun, Path = "tool.sh"});
            app.Dependencies.Add(new() {InterfaceUri = ToolUri, Bindings = {new ExecutableInPath {Name = "my-tool"}}});
            selections.Implementations.Add(tool);
        }

        return selections;
    }

    /// <summary>
    /// Creates selections like <see cref="CreateSelections"/>, but with PowerShell scripts run by the system's Windows PowerShell as a native package.
    /// </summary>
    private Selections CreateWindowsSelections()
    {
        var app = CreateImplementation(AppUri, new()
        {
            ["app.ps1"] = """
                [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false
                "APP_HOME=$env:APP_HOME"
                "LIBPATH=$env:LIBPATH"
                "SPECIAL=$env:SPECIAL"
                "PWD=$((Get-Location).ProviderPath)"
                if (Get-Command my-tool -ErrorAction SilentlyContinue) { & my-tool 'tool arg' }
                foreach ($arg in $args) { "arg=$arg" }
                """,
            ["sub/readme.txt"] = "working directory"
        });
        app.Bindings.Add(new EnvironmentBinding {Name = "APP_HOME", Insert = "", Mode = EnvironmentMode.Replace});
        app.Bindings.Add(new EnvironmentBinding {Name = "LIBPATH", Insert = "lib", Mode = EnvironmentMode.Prepend, Default = "default"});
        app.Bindings.Add(new EnvironmentBinding {Name = "SPECIAL", Value = Special, Mode = EnvironmentMode.Replace});
        app.Bindings.Add(new EnvironmentBinding {Name = "ITEMS", Value = "x;y y", Mode = EnvironmentMode.Replace});
        app.Commands.Add(new()
        {
            Name = Command.NameRun,
            Path = "app.ps1",
            Arguments = {"--fixed", "$SPECIAL", new ForEachArgs {ItemFrom = "ITEMS", Arguments = {"-i", "${item}"}}},
            Runner = new() {InterfaceUri = RunnerUri, Arguments = {"-File"}},
            WorkingDir = new() {Source = "sub"}
        });

        var tool = CreateImplementation(ToolUri, new()
        {
            ["tool.ps1"] = """foreach ($arg in $args) { "tool=$arg" }"""
        });
        tool.Commands.Add(new() {Name = Command.NameRun, Path = "tool.ps1", Runner = new() {InterfaceUri = RunnerUri, Arguments = {"-File"}}});
        app.Dependencies.Add(new() {InterfaceUri = ToolUri, Bindings = {new ExecutableInPath {Name = "my-tool"}}});

        var powerShell = new ImplementationSelection
        {
            InterfaceUri = RunnerUri,
            ID = "package:windows:powershell:5.1",
            Version = new("5.1"),
            Commands =
            {
                new()
                {
                    Name = Command.NameRun,
                    Path = PowerShellPath.Replace('\\', '/'),
                    Arguments = {"-NoProfile", "-ExecutionPolicy", "Bypass"}
                }
            }
        };

        return new()
        {
            InterfaceUri = AppUri,
            Command = Command.NameRun,
            Implementations = {app, tool, powerShell}
        };
    }

    private static string PowerShellPath
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");

    /// <summary>
    /// Creates an implementation in the fake store, named after its manifest digest like in a real store. Files ending in <c>.sh</c> are marked as executable.
    /// </summary>
    private ImplementationSelection CreateImplementation(FeedUri interfaceUri, Dictionary<string, string> files)
    {
        string buildDir = Path.Combine(_storeDir, "build");
        foreach ((string relativePath, string content) in files)
        {
            string path = Path.Combine(buildDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content.Replace("\r\n", "\n") + "\n");
            if (path.EndsWith(".sh")) ImplFileUtils.SetExecutable(path);
        }

        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        new SilentTaskHandler().RunTask(new ReadDirectory(buildDir, builder));
        string digest = builder.Manifest.CalculateDigest();

        string implementationDir = Path.Combine(_storeDir, digest);
        if (Directory.Exists(implementationDir)) Directory.Delete(buildDir, recursive: true);
        else
        {
            Directory.Move(buildDir, implementationDir);
            builder.Manifest.Save(Path.Combine(implementationDir, Manifest.ManifestFile));
        }

        return new() {InterfaceUri = interfaceUri, ID = digest, Version = new("1.0"), ManifestDigest = new(digest)};
    }

    private static string[] Run(string path, params string[] args)
        => Run(new ProcessStartInfo(path, args.JoinEscapeArguments()));

    private static string[] RunPowerShell(string path, params string[] args)
        => Run(new ProcessStartInfo(PowerShellPath, new[] {"-NoProfile", "-ExecutionPolicy", "Bypass", "-File", path}.Concat(args).JoinEscapeArguments()));

    private static string[] Run(ProcessStartInfo startInfo)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        startInfo.EnvironmentVariables.Remove("LIBPATH");

        using var process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, because: error);

        return output.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
    }

    private static void VerifyOutput(string[] output, string targetDir, Selections selections)
    {
        string launcher = File.ReadAllText(Path.Combine(targetDir, "my app"));
        using var scope = new AssertionScope($"generated launcher:\n{launcher}\n");

        // Compare by name rather than full path, since the script resolves symlinks in the target path
        string realAppHome = output.Single(x => x.StartsWith("APP_HOME=")).Substring("APP_HOME=".Length);
        realAppHome.Should().EndWith($"/{Path.GetFileName(targetDir)}/{selections[AppUri].ID}", because: "APP_HOME should point to the materialized copy");
        File.Exists(Path.Combine(realAppHome, "app.sh")).Should().BeTrue();

        output.Should().Equal(
            "runner=runner arg v1",
            $"APP_HOME={realAppHome}",
            $"LIBPATH={realAppHome}/lib:default",
            $"SPECIAL={Special}",
            $"PWD={realAppHome}/sub",
            "tool=tool arg",
            "arg=--fixed",
            $"arg={Special}",
            "arg=-i",
            "arg=x",
            "arg=-i",
            "arg=y y",
            "arg=user arg",
            "arg=");
    }

    private static void VerifyWindowsOutput(string[] output, string targetDir, Selections selections)
    {
        string launcher = File.ReadAllText(Path.Combine(targetDir, "my app.ps1"));
        using var scope = new AssertionScope($"generated launcher:\n{launcher}\n");

        string appHome = Path.Combine(targetDir, selections[AppUri].ID);
        output.Should().Equal(
            $"APP_HOME={appHome}",
            $@"LIBPATH={appHome}\lib;default",
            $"SPECIAL={Special}",
            $@"PWD={appHome}\sub",
            "tool=tool arg",
            "arg=--fixed",
            $"arg={Special}",
            "arg=-i",
            "arg=x",
            "arg=-i",
            "arg=y y",
            "arg=user arg",
            $"arg={Special}",
            "arg=");
    }
}
