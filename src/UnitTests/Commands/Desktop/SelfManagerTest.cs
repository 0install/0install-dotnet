// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.Deployment;
using ZeroInstall.Store.Manifests;

#if NET
using System.Diagnostics;
using System.Runtime.Versioning;
#endif

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Prevents other tests from running in parallel with <see cref="SelfManagerTest"/>, which overrides the process-wide <see cref="Locations.InstallBase"/>.
/// </summary>
[CollectionDefinition(nameof(SelfManagerTestCollection), DisableParallelization = true)]
public class SelfManagerTestCollection;

/// <summary>
/// Contains test methods for <see cref="SelfManager"/>.
/// </summary>
[Collection(nameof(SelfManagerTestCollection))]
public class SelfManagerTest : DirectoryOperationTestBase
{
    private readonly string _originalInstallBase = Locations.InstallBase;
    private readonly TemporaryDirectory _targetParent = new("0install-test-target");
    private readonly string _targetDir;

    public SelfManagerTest()
    {
        _targetDir = Path.Combine(_targetParent, "target");
        Locations.OverrideInstallBase(TempDir);
    }

    public override void Dispose()
    {
        Locations.OverrideInstallBase(_originalInstallBase);
        _targetParent.Dispose();
        base.Dispose();
    }

    private string InTarget(params string[] relativePath)
        => Path.Combine([_targetDir, ..relativePath]);

    private SelfManager CreateManager(bool machineWide = false, bool portable = true)
        => new(_targetDir, new SilentTaskHandler(), machineWide, portable);

    private void Deploy(bool machineWide = false, bool portable = true)
    {
        using var manager = CreateManager(machineWide, portable);
        manager.Deploy();
    }

    private void Remove(bool machineWide = false, bool portable = true)
    {
        using var manager = CreateManager(machineWide, portable);
        manager.Remove();
    }

    private void ShouldHaveNoTempFiles()
        => Directory.GetFiles(_targetDir, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();

    private void ShouldHaveReleasedUpdateMutex()
        => AppMutex.Probe(ZeroInstallEnvironment.UpdateMutexName(_targetDir)).Should().BeFalse(because: "New instances should not stay blocked.");

    /// <summary>
    /// Makes the deployment source invalid by listing a file in its manifest that does not exist.
    /// </summary>
    private void BreakSource()
    {
        Manifest.Save(Path.Combine(TempDir, Manifest.ManifestFile));
        File.Delete(File2Path);
    }

    /// <summary>
    /// Makes sure that <paramref name="action"/> keeps waiting while an instance of Zero Install is running in the target directory, until it is canceled.
    /// </summary>
    private void ShouldWaitForRunningInstance(Action<SelfManager> action)
    {
        using var cancellation = new CancellationTokenSource();
        using var manager = new SelfManager(_targetDir, new CancelableTaskHandler(cancellation.Token), portable: true);
        using var runningInstance = AppMutex.Create(ZeroInstallEnvironment.MutexName(_targetDir));
        Assert.SkipUnless(AppMutex.Probe(ZeroInstallEnvironment.MutexName(_targetDir)), "AppMutex is not supported on this platform by this version of NanoByte.Common");

        cancellation.CancelAfter(200);
        Assert.ThrowsAny<OperationCanceledException>(() => action(manager));
    }

    private sealed class CancelableTaskHandler(CancellationToken cancellationToken) : SilentTaskHandler, ITaskHandler
    {
        CancellationToken ITaskHandler.CancellationToken => cancellationToken;
    }

    [Fact]
    public void PortableAndMachineWideAreExclusive()
        => Assert.Throws<ArgumentException>(() => new SelfManager(_targetDir, new SilentTaskHandler(), machineWide: true, portable: true));

    [Fact]
    public void OnlyOneOperationAtATime()
    {
        Assert.SkipWhen(UnixUtils.IsUnix, "WaitHandleExtensions.WaitOne() from NanoByte.Common does not throw a TimeoutException for mutexes on Unix");

        Exception? exception = null;

        using (CreateManager())
        {
            // Mutexes are re-entrant within the same thread
            var thread = new Thread(() =>
            {
                try
                {
                    using var other = CreateManager();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.Start();
            thread.Join();
        }

        exception.Should().BeOfType<UnauthorizedAccessException>();
    }

    [Fact]
    public void DeployPortable()
    {
        var modifiedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.WriteAllText(File1Path, "content");
        File.SetLastWriteTimeUtc(File1Path, modifiedTime);

        Deploy();

        File.ReadAllText(InTarget("file1")).Should().Be("content");
        File.GetLastWriteTimeUtc(InTarget("file1")).Should().Be(modifiedTime);
        File.Exists(InTarget("subdir", "file2")).Should().BeTrue();
        File.Exists(InTarget(Manifest.ManifestFile)).Should().BeTrue();
        File.Exists(InTarget(Locations.PortableFlagName)).Should().BeTrue();
        ShouldHaveNoTempFiles();
        ShouldHaveReleasedUpdateMutex();

        File.Exists(Path.Combine(TempDir, Manifest.ManifestFile)).Should().BeFalse(because: "The source should not be modified.");
    }

    [Fact]
    public void DeployPortableAsLibraryIsRejected()
    {
        using var manager = CreateManager();

        Assert.Throws<ArgumentException>(() => manager.Deploy(libraryMode: true));

        Directory.Exists(_targetDir).Should().BeFalse();
    }

    [Fact]
    public void DeployOverExistingVersion()
    {
        File.WriteAllText(File1Path, "old");
        Deploy();
        FileUtils.Touch(InTarget("untracked"));

        File.WriteAllText(File1Path, "new");
        File.Delete(File2Path);
        Directory.Delete(SubdirPath);
        FileUtils.Touch(Path.Combine(TempDir, "file3"));
        Deploy();

        File.ReadAllText(InTarget("file1")).Should().Be("new", because: "Changed files should be updated.");
        File.Exists(InTarget("file3")).Should().BeTrue(because: "New files should be added.");
        Directory.Exists(InTarget("subdir")).Should().BeFalse(because: "Obsolete files and directories should be removed.");
        File.Exists(InTarget("untracked")).Should().BeTrue(because: "Files not in the old manifest should be kept.");
        ShouldHaveNoTempFiles();
    }

    [Fact]
    public void DeployToInstallBaseCopiesNothing()
    {
        var entries = Directory.GetFileSystemEntries(TempDir);

        using (var manager = new SelfManager(TempDir, new SilentTaskHandler(), portable: true))
            manager.Deploy();

        Directory.GetFileSystemEntries(TempDir).Should().BeEquivalentTo(entries);
    }

    [Fact]
    public void DeployFailureKeepsExistingVersion()
    {
        File.WriteAllText(File1Path, "old");
        Deploy();
        string oldManifest = File.ReadAllText(InTarget(Manifest.ManifestFile));

        File.WriteAllText(File1Path, "new");
        BreakSource();

        Assert.ThrowsAny<IOException>(() => Deploy());

        File.ReadAllText(InTarget("file1")).Should().Be("old");
        File.Exists(InTarget("subdir", "file2")).Should().BeTrue();
        File.ReadAllText(InTarget(Manifest.ManifestFile)).Should().Be(oldManifest);
        ShouldHaveNoTempFiles();
        ShouldHaveReleasedUpdateMutex();
    }

    [Fact]
    public void DeployWaitsForRunningInstance()
    {
        ShouldWaitForRunningInstance(manager => manager.Deploy());

        Directory.Exists(_targetDir).Should().BeFalse();
        ShouldHaveReleasedUpdateMutex();
    }

    [Fact]
    public void RemovePortable()
    {
        Deploy();

        Remove();

        Directory.Exists(_targetDir).Should().BeFalse(because: "Entire directory should be gone, including the portable flag.");
        File.Exists(File1Path).Should().BeTrue(because: "The source should not be modified.");
        ShouldHaveReleasedUpdateMutex();
    }

    [Fact]
    public void RemoveKeepsUntrackedFiles()
    {
        Deploy();
        string untrackedPath = InTarget("subdir", "untracked");
        FileUtils.Touch(untrackedPath);

        Remove();

        Directory.GetFileSystemEntries(InTarget("subdir")).Should().Equal(untrackedPath);
        Directory.GetFileSystemEntries(_targetDir).Should().Equal(InTarget("subdir"));
    }

    [Fact]
    public void RemoveWithoutManifestDeletesEverything()
    {
        Directory.CreateDirectory(InTarget("subdir"));
        FileUtils.Touch(InTarget("file"));
        FileUtils.Touch(InTarget("subdir", "file"));

        Remove();

        Directory.Exists(_targetDir).Should().BeFalse(because: "A directory without a manifest is assumed to contain only files that should be removed.");
    }

    [Fact]
    public void RemoveWaitsForRunningInstance()
    {
        Deploy();

        ShouldWaitForRunningInstance(manager => manager.Remove());

        File.Exists(InTarget("file1")).Should().BeTrue();
        File.Exists(InTarget(Manifest.ManifestFile)).Should().BeTrue();
        ShouldHaveNoTempFiles();
        ShouldHaveReleasedUpdateMutex();
    }

#if NET
    [Fact]
    public void DeployNonPortable()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Would modify the system on Windows");

        Deploy(portable: false);

        File.Exists(InTarget("file1")).Should().BeTrue();
        File.Exists(InTarget(Locations.PortableFlagName)).Should().BeFalse();
    }

    [Fact, UnsupportedOSPlatform("windows")]
    public void DeployExecutableAndSymlink()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests Unix file permissions and symlinks");

        string executablePath = Path.Combine(TempDir, "executable");
        FileUtils.Touch(executablePath);
        File.SetUnixFileMode(executablePath, (UnixFileMode)0b111_101_101);
        File.CreateSymbolicLink(Path.Combine(TempDir, "link"), "file1");

        Deploy();

        (File.GetUnixFileMode(InTarget("executable")) & UnixFileMode.UserExecute).Should().Be(UnixFileMode.UserExecute);
        (File.GetUnixFileMode(InTarget("file1")) & UnixFileMode.UserExecute).Should().Be(UnixFileMode.None);
        FileUtils.IsSymlink(InTarget("link"), out string? target).Should().BeTrue();
        target.Should().Be("file1");
    }

    [Fact, UnsupportedOSPlatform("windows")]
    public void DeployMachineWideIsAccessibleToAllUsers()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests Unix file permissions; would modify the system on Windows");

        const UnixFileMode readable = (UnixFileMode)0b110_100_100, executable = (UnixFileMode)0b111_101_101;

        string executablePath = Path.Combine(TempDir, "executable");
        FileUtils.Touch(executablePath);
        File.SetUnixFileMode(executablePath, (UnixFileMode)0b111_000_000);
        File.SetUnixFileMode(File1Path, (UnixFileMode)0b110_000_000);
        File.SetUnixFileMode(SubdirPath, (UnixFileMode)0b111_000_000);

        Deploy(machineWide: true, portable: false);

        File.GetUnixFileMode(_targetDir).Should().Be(executable);
        File.GetUnixFileMode(InTarget("subdir")).Should().Be(executable);
        File.GetUnixFileMode(InTarget("file1")).Should().Be(readable);
        File.GetUnixFileMode(InTarget("executable")).Should().Be(executable);
        File.GetUnixFileMode(InTarget(Manifest.ManifestFile)).Should().Be(readable);
    }

    [Fact]
    public void DeleteCommandLine()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests a shell script used on Unix");

        FileUtils.Touch(Path.Combine(TempDir, Manifest.ManifestFile));

        RunDeleteCommandLine().Should().Be(0);

        Directory.Exists(TempDir).Should().BeFalse(because: "Entire directory should be gone after running the command.");
    }

    [Fact]
    public void DeleteCommandLineUntrackedFiles()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests a shell script used on Unix");

        string untrackedPath = Path.Combine(SubdirPath, "untracked");
        FileUtils.Touch(untrackedPath);

        RunDeleteCommandLine().Should().Be(0, because: "Directories that are not empty are not an error.");

        Directory.GetFileSystemEntries(SubdirPath).Should().Equal(untrackedPath);
        Directory.GetFileSystemEntries(TempDir).Should().Equal(SubdirPath);
    }

    [Fact, UnsupportedOSPlatform("windows")]
    public void DeleteCommandLineFailure()
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Tests a shell script used on Unix");
        Assert.SkipWhen(Environment.IsPrivilegedProcess, "File permissions do not apply to root");

        var pendingDeletes = StagePendingDeletes();

        File.SetUnixFileMode(SubdirPath, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            Run(pendingDeletes).Should().Be((int)ExitCode.IOError);
        }
        finally
        {
            File.SetUnixFileMode(SubdirPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private int RunDeleteCommandLine() => Run(StagePendingDeletes());

    /// <summary>
    /// Stages the clearing of the test directory.
    /// The operation is not disposed, just like when replacing the process with the command, since that would try to roll back.
    /// </summary>
    private List<string> StagePendingDeletes()
    {
        var operation = new ClearDirectory(TempDir, Manifest, new SilentTaskHandler());
        operation.Stage();
        return operation.PendingDeletes.ToList();
    }

    private static int Run(IEnumerable<string> paths)
    {
        string[] commandLine = SelfManager.GetDeleteCommandLine(paths);
        var startInfo = new ProcessStartInfo(commandLine[0], commandLine.Skip(1)) {UseShellExecute = false, WorkingDirectory = "/"};

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
        return process.ExitCode;
    }
#endif
}
