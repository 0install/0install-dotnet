// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Services;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Contains test methods for <see cref="DiskImplementationStore"/>.
    /// </summary>
    [Collection("WorkingDir")]
    public class DiskImplementationStoreTest : IDisposable
    {
        private readonly MockTaskHandler _handler;
        private readonly TemporaryDirectory _tempDir;
        private DiskImplementationStore _implementationStore;

        public DiskImplementationStoreTest()
        {
            _handler = new MockTaskHandler();
            _tempDir = new TemporaryDirectory("0install-unit-tests");
            _implementationStore = new DiskImplementationStore(_tempDir);
        }

        public void Dispose()
        {
            _implementationStore.Purge(_handler);
            _tempDir.Dispose();
        }

        [Fact]
        public void TestContains()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "sha256new_123ABC"));
            _implementationStore.Contains(new ManifestDigest(sha256New: "123ABC")).Should().BeTrue();
            _implementationStore.Contains(new ManifestDigest(sha256New: "456XYZ")).Should().BeFalse();
        }

        [Fact]
        public void TestListAll()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "sha1=test1"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "sha1new=test2"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "sha256=test3"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "sha256new_test4"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "temp=stuff"));
            _implementationStore.ListAll().Should().BeEquivalentTo(
                new ManifestDigest(sha1: "test1"),
                new ManifestDigest(sha1New: "test2"),
                new ManifestDigest(sha256: "test3"),
                new ManifestDigest(sha256New: "test4"));
        }

        [Fact]
        public void TestListAllTemp()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "sha1=test"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "temp=stuff"));
            _implementationStore.ListAllTemp().Should().Equal(Path.Combine(_tempDir, "temp=stuff"));
        }

        private string DeployPackage(string id, TestRoot root)
        {
            string path = Path.Combine(_tempDir, id);
            root.Build(path);
            ManifestTest.CreateDotFile(path, ManifestFormat.FromPrefix(id), _handler);
            FileUtils.EnableWriteProtection(path);
            return path;
        }

        [Fact]
        public void ShouldHardlinkIdenticalFilesInSameImplementation()
        {
            string package1Path = DeployPackage("sha256=1", new TestRoot
            {
                new TestFile("fileA") {Contents = "abc"},
                new TestDirectory("dir") {new TestFile("fileB") {Contents = "abc"}}
            });

            _implementationStore.Optimise(_handler).Should().Be(3);
            _implementationStore.Optimise(_handler).Should().Be(0);
            FileUtils.AreHardlinked(
                Path.Combine(package1Path, "fileA"),
                Path.Combine(package1Path, "dir", "fileB")).Should().BeTrue();
        }

        [Fact]
        public void ShouldHardlinkIdenticalFilesInDifferentImplementations()
        {
            string package1Path = DeployPackage("sha256=1", new TestRoot {new TestFile("fileA") {Contents = "abc"}});
            string package2Path = DeployPackage("sha256=2", new TestRoot {new TestFile("fileA") {Contents = "abc"}});

            _implementationStore.Optimise(_handler).Should().Be(3);
            _implementationStore.Optimise(_handler).Should().Be(0);
            FileUtils.AreHardlinked(
                Path.Combine(package1Path, "fileA"),
                Path.Combine(package2Path, "fileA")).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotHardlinkFilesWithDifferentTimestamps()
        {
            string package1Path = DeployPackage("sha256=1", new TestRoot
            {
                new TestFile("fileA") {Contents = "abc", LastWrite = new DateTime(2000, 1, 1)},
                new TestFile("fileX") {Contents = "abc", LastWrite = new DateTime(2000, 2, 2)}
            });

            _implementationStore.Optimise(_handler).Should().Be(0);
            FileUtils.AreHardlinked(
                Path.Combine(package1Path, "fileA"),
                Path.Combine(package1Path, "fileX")).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotHardlinkFilesWithDifferentContent()
        {
            string package1Path = DeployPackage("sha256=1", new TestRoot {new TestFile("fileA") {Contents = "abc"}});
            string package2Path = DeployPackage("sha256=2", new TestRoot {new TestFile("fileA") {Contents = "def"}});

            _implementationStore.Optimise(_handler).Should().Be(0);
            FileUtils.AreHardlinked(
                Path.Combine(package1Path, "fileA"),
                Path.Combine(package2Path, "fileA")).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotHardlinkAcrossManifestFormatBorders()
        {
            string package1Path = DeployPackage("sha256=1", new TestRoot {new TestFile("fileA") {Contents = "abc"}});
            string package2Path = DeployPackage("sha256new_2", new TestRoot {new TestFile("fileA") {Contents = "abc"}});

            _implementationStore.Optimise(_handler).Should().Be(0);
            FileUtils.AreHardlinked(
                Path.Combine(package1Path, "fileA"),
                Path.Combine(package2Path, "fileA")).Should().BeFalse();
        }

        [Fact]
        public void ShouldAllowToAddFolder()
        {
            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                var digest = new ManifestDigest(ManifestTest.CreateDotFile(testDir, ManifestFormat.Sha256, _handler));
                _implementationStore.AddDirectory(testDir, digest, _handler);

                _implementationStore.Contains(digest).Should().BeTrue(because: "After adding, Store must contain the added package");
                _implementationStore.ListAll().Should().Equal(new[] {digest}, because: "After adding, Store must show the added package in the complete list");
            }
        }

        [Fact]
        public void ShouldRecreateMissingStoreDir()
        {
            Directory.Delete(_tempDir, recursive: true);

            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                var digest = new ManifestDigest(ManifestTest.CreateDotFile(testDir, ManifestFormat.Sha256, _handler));
                _implementationStore.AddDirectory(testDir, digest, _handler);

                _implementationStore.Contains(digest).Should().BeTrue(because: "After adding, Store must contain the added package");
                _implementationStore.ListAll().Should().Equal(new[] {digest}, because: "After adding, Store must show the added package in the complete list");

                Directory.Exists(_tempDir).Should().BeTrue(because: "Store directory should have been recreated");
            }
        }

        [Fact]
        public void ShouldHandleRelativePaths()
        {
            // Change the working directory
            string oldWorkingDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _tempDir;

            try
            {
                _implementationStore = new DiskImplementationStore(".");
                ShouldAllowToAddFolder();
            }
            finally
            {
                // Restore the original working directory
                Environment.CurrentDirectory = oldWorkingDir;
            }
        }

        [Fact]
        public void ShouldAllowToRemove()
        {
            string implPath = Path.Combine(_tempDir, "sha256new_123ABC");
            Directory.CreateDirectory(implPath);

            _implementationStore.Remove(new ManifestDigest(sha256New: "123ABC"), _handler);
            Directory.Exists(implPath).Should().BeFalse(because: "After remove, Store may no longer contain the added package");
        }

        [Fact]
        public void ShouldReturnCorrectPathOfPackageInCache()
        {
            string implPath = Path.Combine(_tempDir, "sha256new_123ABC");
            Directory.CreateDirectory(implPath);
            _implementationStore.GetPath(new ManifestDigest(sha256New: "123ABC"))
                  .Should().Be(implPath, because: "Store must return the correct path for Implementations it contains");
        }

        [Fact]
        public void ShouldThrowWhenRequestedPathOfUncontainedPackage()
            => _implementationStore.GetPath(new ManifestDigest(sha256: "123")).Should().BeNull();

        [Fact]
        public void TestAuditPass()
        {
            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                new TestRoot {new TestFile("file") {Contents = "AAA"}}.Build(testDir);
                var digest = new ManifestDigest(ManifestTest.CreateDotFile(testDir, ManifestFormat.Sha1New, _handler));
                _implementationStore.AddDirectory(testDir, digest, _handler);

                _implementationStore.Verify(digest, _handler);
                _handler.LastQuestion.Should().BeNull();
            }
        }

        [Fact]
        public void TestAuditFail()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "sha1new=abc"));
            _implementationStore.Contains(new ManifestDigest(sha1New: "abc")).Should().BeTrue();

            _handler.AnswerQuestionWith = true;
            _implementationStore.Verify(new ManifestDigest(sha1New: "abc"), _handler);
            _handler.LastQuestion.Should().Be(
                string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedAskRemove, "sha1new=abc"));

            _implementationStore.Contains(new ManifestDigest(sha1New: "abc")).Should().BeFalse();
        }

        [Fact]
        public void StressTest()
        {
            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                new TestRoot {new TestFile("file") {Contents = "AAA"}}.Build(testDir);

                var digest = new ManifestDigest(ManifestTest.CreateDotFile(testDir, ManifestFormat.Sha256, _handler));

                Exception exception = null;
                var threads = new Thread[100];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        try
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            _implementationStore.AddDirectory(testDir, digest, _handler);
                            _implementationStore.Remove(digest, _handler);
                        }
                        catch (ImplementationAlreadyInStoreException)
                        {}
                        catch (ImplementationNotFoundException)
                        {}
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    });
                    threads[i].Start();
                }

                foreach (var thread in threads)
                    thread.Join();
                if (exception != null) throw exception.PreserveStack();

                _implementationStore.Contains(digest).Should().BeFalse();
            }
        }
    }
}
