// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Security.Cryptography;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// Contains test methods for <see cref="ManifestGenerator"/>.
    /// </summary>
    public class ManifestGeneratorTest
    {
        private static readonly string _hash = TestFile.DefaultContents.Hash(SHA1.Create());

        [Fact]
        public void TestFileOrder() => Test(
            new TestRoot {new TestFile("x"), new TestFile("y"), new TestFile("Z")},
            new ManifestNormalFile(_hash, TestFile.DefaultLastWrite, TestFile.DefaultContents.Length, "Z"),
            new ManifestNormalFile(_hash, TestFile.DefaultLastWrite, TestFile.DefaultContents.Length, "x"),
            new ManifestNormalFile(_hash, TestFile.DefaultLastWrite, TestFile.DefaultContents.Length, "y"));

        [Fact]
        public void TestFileTypes() => Test(
            new TestRoot
            {
                new TestFile("executable") {IsExecutable = true},
                new TestFile("normal"),
                new TestSymlink("symlink", target: "abc"),
                new TestDirectory("dir") {new TestFile("sub")}
            },
            new ManifestExecutableFile(_hash, TestFile.DefaultLastWrite, TestFile.DefaultContents.Length, "executable"),
            new ManifestNormalFile(_hash, TestFile.DefaultLastWrite, TestFile.DefaultContents.Length, "normal"),
            new ManifestSymlink("abc".Hash(SHA1.Create()), TestFile.DefaultContents.Length, "symlink"),
            new ManifestDirectory("/dir"),
            new ManifestNormalFile(_hash, TestFile.DefaultLastWrite, TestFile.DefaultContents.Length, "sub"));

        private static void Test(TestRoot root, params ManifestNode[] expected)
        {
            using (var sourceDirectory = new TemporaryDirectory("0install-unit-tests"))
            {
                root.Build(sourceDirectory);
                var generator = new ManifestGenerator(sourceDirectory, ManifestFormat.Sha1New);
                generator.Run();
                generator.Manifest.Should().Equal(expected);
            }
        }
    }
}
