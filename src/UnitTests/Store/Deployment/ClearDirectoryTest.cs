// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Deployment;

/// <summary>
/// Contains test methods for <see cref="ClearDirectory"/>.
/// </summary>
public class ClearDirectoryTest : DirectoryOperationTestBase
{
    [Fact]
    public void StageAndCommit()
    {
        string manifestPath = Path.Combine(TempDir, Manifest.ManifestFile);
        FileUtils.Touch(manifestPath); // Content of Manifest file is not read, in-memory Manifest is used instead

        using (var operation = new ClearDirectory(TempDir, Manifest, new SilentTaskHandler()))
        {
            operation.Stage();
            File.Exists(manifestPath).Should().BeFalse(because: "Original manifest file should be gone after staging.");
            File.Exists(File1Path).Should().BeFalse(because: "Original file should be gone after staging.");
            Directory.Exists(SubdirPath).Should().BeTrue(because: "Directories should be left intact after staging.");
            File.Exists(File2Path).Should().BeFalse(because: "Original file should be gone after staging.");
            Directory.GetFileSystemEntries(TempDir).Length.Should().Be(3, because: "Backup files should be preset after staging.");
            Directory.GetFileSystemEntries(SubdirPath).Length.Should().Be(1, because: "Backup files should be preset after staging.");

            operation.Commit();
        }

        Directory.Exists(TempDir).Should().BeFalse(because: "Entire directory should be gone after commit.");
    }

    [Fact]
    public void StageAndRollBack()
    {
        using (var operation = new ClearDirectory(TempDir, Manifest, new SilentTaskHandler()))
        {
            operation.Stage();
            // Missing .Commit() automatically triggers rollback
        }

        File.Exists(File1Path).Should().BeTrue(because: "Original file should be back after rollback.");
        File.Exists(File2Path).Should().BeTrue(because: "Original file should be back after rollback.");
    }

    [Fact]
    public void ReadOnlyAttribute()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Read-only file attribute is only available on Windows");

        new FileInfo(File1Path).IsReadOnly = true;

        using (var operation = new ClearDirectory(TempDir, Manifest, new SilentTaskHandler()))
        {
            operation.Stage();
            operation.Commit();
        }

        Directory.Exists(TempDir).Should().BeFalse(because: "Entire directory should be gone after commit.");
    }

    [Fact]
    public void UntrackedFilesInTarget()
    {
        FileUtils.Touch(Path.Combine(TempDir, "untracked"));

        using (var operation = new ClearDirectory(TempDir, Manifest, new SilentTaskHandler()))
        {
            operation.Stage();
            operation.Commit();
        }

        Directory.GetFileSystemEntries(TempDir).Length.Should().Be(1, because: "Only untracked file should be left after commit.");
    }

    [Fact]
    public void DirectorySymlink()
    {
        using var targetDir = BuildWithDirectorySymlink(out var manifest);
        string linkPath = Path.Combine(targetDir, "dir-link");

        using (var operation = new ClearDirectory(targetDir, manifest, new SilentTaskHandler()))
        {
            operation.Stage();
            Directory.Exists(linkPath).Should().BeFalse(because: "Original link should be gone after staging.");
            operation.Commit();
        }

        Directory.Exists(targetDir).Should().BeFalse(because: "Entire directory should be gone after commit.");
    }

    [Fact]
    public void DirectorySymlinkRollBack()
    {
        using var targetDir = BuildWithDirectorySymlink(out var manifest);
        string linkPath = Path.Combine(targetDir, "dir-link");

        using (var operation = new ClearDirectory(targetDir, manifest, new SilentTaskHandler()))
        {
            operation.Stage();
            // Missing .Commit() automatically triggers rollback
        }

        FileUtils.IsSymlink(linkPath, out string? target).Should().BeTrue(because: "Original link should be back after rollback.");
        target.Should().Be("dir");
        Directory.GetFileSystemEntries(targetDir).Length.Should().Be(2, because: "No backup should be left after rollback.");
    }

    /// <summary>
    /// Builds an implementation containing a directory and a symlink pointing to it.
    /// </summary>
    private static TemporaryDirectory BuildWithDirectorySymlink(out Manifest manifest)
    {
        var targetDir = new TemporaryDirectory("0install-test-dir");
        var manifestBuilder = new ManifestBuilder(ManifestFormat.Sha256New);
        var builder = new DirectoryBuilder(targetDir, manifestBuilder);
        builder.AddDirectory("dir");
        builder.AddSymlink("dir-link", "dir");
        if (!FileUtils.IsSymlink(Path.Combine(targetDir, "dir-link")))
        {
            targetDir.Dispose();
            Assert.Skip("Creating NTFS symlinks requires Developer Mode or administrator rights");
        }

        manifest = manifestBuilder.Manifest;
        return targetDir;
    }

    [Fact]
    public void MissingFiles()
    {
        TempDir.Dispose();

        using var operation = new ClearDirectory(TempDir, Manifest, new SilentTaskHandler());
        operation.Stage();
        operation.Commit();
    }
}
