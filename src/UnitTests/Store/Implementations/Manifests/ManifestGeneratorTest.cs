// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// Contains test methods for <see cref="ManifestGenerator"/>.
    /// </summary>
    public class ManifestGeneratorTest : TestWithMocks
    {
        #region Helpers
        private static Manifest GenerateManifest(string path, ManifestFormat format)
        {
            var generator = new ManifestGenerator(path, format);
            generator.Run();
            return generator.Manifest;
        }

        private static Manifest GenerateManifest(TestRoot root)
        {
            using var tempDir = new TemporaryDirectory("0install-test-source");
            root.Build(tempDir);
            return GenerateManifest(tempDir, ManifestFormat.Sha1New);
        }

        public static string CreateDotFile(string path, ManifestFormat format, ITaskHandler? _ = null)
            => GenerateManifest(path, format).Save(Path.Combine(path, Manifest.ManifestFile));
        #endregion

        private static readonly string _hash = TestFile.DefaultContents.Hash(SHA1.Create());
        private readonly long _lastWriteTime = TestFile.DefaultLastWrite.ToUnixTime();

        [Fact]
        public void FileTypes()
        {
            var manifest = GenerateManifest(new TestRoot
            {
                new TestFile("executable") {IsExecutable = true},
                new TestFile("normal"),
                new TestSymlink("symlink", target: "abc"),
                new TestDirectory("dir") {new TestFile("sub")}
            });

            manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
            {
                [""] =
                {
                    ["executable"] = new ManifestExecutableFile(_hash, _lastWriteTime, TestFile.DefaultContents.Length),
                    ["normal"] = new ManifestNormalFile(_hash, _lastWriteTime, TestFile.DefaultContents.Length),
                    ["symlink"] = new ManifestSymlink("abc".Hash(SHA1.Create()), TestFile.DefaultContents.Length),
                },
                ["dir"] =
                {
                    ["sub"] = new ManifestNormalFile(_hash, _lastWriteTime, TestFile.DefaultContents.Length)
                }
            });
        }

        [Fact]
        public void CalculateDigest()
        {
            using var testDir = new TemporaryDirectory("0install-test-impl");
            new TestRoot
            {
                new TestDirectory("subdir") {new TestFile("file")}
            }.Build(testDir);

            GenerateManifest(testDir, ManifestFormat.Sha1New)
               .CalculateDigest()
               .Should().Be(CreateDotFile(testDir, ManifestFormat.Sha1New),
                    because: "sha1new dot file and digest should match");
            GenerateManifest(testDir, ManifestFormat.Sha256)
               .CalculateDigest()
               .Should().Be(CreateDotFile(testDir, ManifestFormat.Sha256),
                    because: "sha256 dot file and digest should match");
            GenerateManifest(testDir, ManifestFormat.Sha256New)
               .CalculateDigest()
               .Should().Be(CreateDotFile(testDir, ManifestFormat.Sha256New),
                    because: "sha256new dot file and digest should match");
        }

        // ReSharper disable AssignNullToNotNullAttribute
        [Fact]
        public void ShouldListNormalWindowsExeWithFlagF()
        {
            using var package = new TemporaryDirectory("0install-test-impl");
            string filePath = Path.Combine(package, "test.exe");
            string manifestPath = Path.Combine(package, Manifest.ManifestFile);

            File.WriteAllText(filePath, @"xxxxxxx");
            CreateDotFile(package, ManifestFormat.Sha256);

            using var manifest = File.OpenText(manifestPath);
            string firstLine = manifest.ReadLine();
            Assert.True(Regex.IsMatch(firstLine, @"^F \w+ \d+ \d+ test.exe$"), "Manifest didn't match expected format");
        }

        [Fact]
        public void ShouldListFilesInXbitWithFlagX()
        {
            using var package = new TemporaryDirectory("0install-test-impl");
            string filePath = Path.Combine(package, "test.exe");
            string manifestPath = Path.Combine(package, Manifest.ManifestFile);

            File.WriteAllText(filePath, "target");
            if (WindowsUtils.IsWindows)
            {
                string flagPath = Path.Combine(package, FlagUtils.XbitFile);
                File.WriteAllText(flagPath, @"/test.exe");
            }
            else FileUtils.SetExecutable(filePath, true);
            CreateDotFile(package, ManifestFormat.Sha256);

            using var manifest = File.OpenText(manifestPath);
            string? firstLine = manifest.ReadLine();
            Assert.True(Regex.IsMatch(firstLine, @"^X \w+ \d+ \d+ test.exe$"), "Manifest didn't match expected format");
        }

        [Fact]
        public void ShouldListFilesInSymlinkWithFlagS()
        {
            using var package = new TemporaryDirectory("0install-test-impl");
            string sourcePath = Path.Combine(package, "test");
            string manifestPath = Path.Combine(package, Manifest.ManifestFile);

            if (WindowsUtils.IsWindows)
            {
                File.WriteAllText(sourcePath, "target");
                string flagPath = Path.Combine(package, FlagUtils.SymlinkFile);
                File.WriteAllText(flagPath, @"/test");
            }
            else FileUtils.CreateSymlink(sourcePath, "target");
            CreateDotFile(package, ManifestFormat.Sha256);

            using var manifest = File.OpenText(manifestPath);
            string? firstLine = manifest.ReadLine();
            Assert.True(Regex.IsMatch(firstLine, @"^S \w+ \d+ test$"), "Manifest didn't match expected format");
        }

        [Fact]
        public void ShouldListNothingForEmptyPackage()
        {
            using var package = new TemporaryDirectory("0install-test-impl");
            CreateDotFile(package, ManifestFormat.Sha256);
            using var manifestFile = File.OpenRead(Path.Combine(package, Manifest.ManifestFile));
            manifestFile.Length.Should().Be(0, because: "Empty package directory should make an empty manifest");
        }

        [Fact]
        public void ShouldHandleSubdirectoriesWithExecutables()
        {
            using var package = new TemporaryDirectory("0install-test-impl");
            string innerPath = Path.Combine(package, "inner");
            Directory.CreateDirectory(innerPath);

            string innerExePath = Path.Combine(innerPath, "inner.exe");
            string manifestPath = Path.Combine(package, Manifest.ManifestFile);
            File.WriteAllText(innerExePath, @"xxxxxxx");
            if (WindowsUtils.IsWindows)
            {
                string flagPath = Path.Combine(package, FlagUtils.XbitFile);
                File.WriteAllText(flagPath, @"/inner/inner.exe");
            }
            else FileUtils.SetExecutable(innerExePath, true);
            CreateDotFile(package, ManifestFormat.Sha256);
            using var manifestFile = File.OpenText(manifestPath);
            manifestFile.ReadLine().Should().MatchRegex(@"^D /inner$");
            manifestFile.ReadLine().Should().MatchRegex(@"^X \w+ \w+ \d+ inner.exe$");
        }
    }
}
