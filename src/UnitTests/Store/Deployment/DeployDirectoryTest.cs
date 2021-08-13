// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Deployment
{
    /// <summary>
    /// Contains test methods for <see cref="DeployDirectory"/>.
    /// </summary>
    public class DeployDirectoryTest : DirectoryOperationTestBase
    {
        private readonly TemporaryDirectory _destinationDirectory = new("0install-test-dest");

        public override void Dispose()
        {
            _destinationDirectory.Dispose();
            base.Dispose();
        }

        private readonly string _destinationManifestPath, _destinationFile1Path, _destinationSubdirPath, _destinationFile2Path;

        public DeployDirectoryTest()
        {
            _destinationManifestPath = Path.Combine(_destinationDirectory, Manifest.ManifestFile);
            _destinationFile1Path = Path.Combine(_destinationDirectory, "file1");
            _destinationSubdirPath = Path.Combine(_destinationDirectory, "subdir");
            _destinationFile2Path = Path.Combine(_destinationSubdirPath, "file2");
        }

        [Fact]
        public void StageAndCommit()
        {
            Directory.Delete(_destinationDirectory);

            using (var operation = new DeployDirectory(TempDir, Manifest, _destinationDirectory, new SilentTaskHandler()))
            {
                operation.Stage();
                File.Exists(_destinationManifestPath).Should().BeFalse(because: "Final destination manifest file should not exist yet after staging.");
                File.Exists(_destinationFile1Path).Should().BeFalse(because: "Final destination file should not exist yet after staging.");
                Directory.Exists(_destinationSubdirPath).Should().BeTrue(because: "Directories should be created after staging.");
                File.Exists(_destinationFile2Path).Should().BeFalse(because: "Final destination file should not exist yet after staging.");
                Directory.GetFileSystemEntries(_destinationDirectory).Length.Should().Be(3, because: "Temp files should be preset after staging.");
                Directory.GetFileSystemEntries(_destinationSubdirPath).Length.Should().Be(1, because: "Temp files should be preset after staging.");

                operation.Commit();
            }

            Manifest.Load(_destinationManifestPath, Manifest.Format).Should().BeEquivalentTo(Manifest, because: "Destination manifest file should equal in-memory manifest used as copy instruction.");
            File.Exists(_destinationManifestPath).Should().BeTrue(because: "Final destination manifest file should exist after commit.");
            File.Exists(_destinationFile1Path).Should().BeTrue(because: "Final destination file should exist after commit.");
            File.Exists(_destinationFile2Path).Should().BeTrue(because: "Final destination file should exist after commit.");
        }

        [Fact]
        public void StageAndRollBack()
        {
            Directory.Delete(_destinationDirectory);

            using (var operation = new DeployDirectory(TempDir, Manifest, _destinationDirectory, new SilentTaskHandler()))
            {
                operation.Stage();
                // Missing .Commit() automatically triggers rollback
            }

            Directory.Exists(_destinationDirectory).Should().BeFalse(because: "Directory should be gone after rollback.");
        }

        [Fact]
        public void PreExistingFiles()
        {
            FileUtils.Touch(Path.Combine(_destinationDirectory, "preexisting"));

            using (var operation = new DeployDirectory(TempDir, Manifest, _destinationDirectory, new SilentTaskHandler()))
            {
                operation.Stage();
                // Missing .Commit() automatically triggers rollback
            }

            Directory.GetFileSystemEntries(_destinationDirectory).Length.Should().Be(1, because: "All new content should be gone after rollback.");
        }

        [SkippableFact]
        public void ReadOnlyAttribute()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "Read-only file attribute is only available on Windows");

            FileUtils.Touch(_destinationFile1Path);
            new FileInfo(_destinationFile1Path).IsReadOnly = true;

            using (var operation = new DeployDirectory(TempDir, Manifest, _destinationDirectory, new SilentTaskHandler()))
            {
                operation.Stage();
                operation.Commit();
            }

            File.Exists(_destinationFile1Path).Should().BeTrue(because: "Final destination file should exist after commit.");
            File.Exists(_destinationFile2Path).Should().BeTrue(because: "Final destination file should exist after commit.");
        }
    }
}
