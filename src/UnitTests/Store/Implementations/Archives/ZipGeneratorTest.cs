// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Values;
using Xunit;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Contains test methods for <see cref="ZipGenerator"/>.
    /// </summary>
    public class ZipGeneratorTest : ArchiveGeneratorTest<ZipGenerator>
    {
        protected override ZipGenerator CreateGenerator(string sourceDirectory, Stream stream) => new ZipGenerator(sourceDirectory, stream);

        [Fact]
        public void TestFileOrder()
        {
            var stream = BuildArchive(new TestRoot {new TestFile("x"), new TestFile("y"), new TestFile("Z")});

            using var archive = new ZipFile(stream);
            archive[0].Name.Should().Be("Z");
            archive[1].Name.Should().Be("x");
            archive[2].Name.Should().Be("y");
        }

        [Fact]
        public void TestFileTypes()
        {
            var stream = BuildArchive(new TestRoot
            {
                new TestFile("executable") {IsExecutable = true},
                new TestFile("normal"),
                new TestSymlink("symlink", target: "abc"),
                new TestDirectory("dir") {new TestFile("sub")}
            });

            using var archive = new ZipFile(stream);
            var executable = archive[0];
            executable.Name.Should().Be("executable");
            executable.IsFile.Should().BeTrue();
            executable.DateTime.Should().Be(TestFile.DefaultLastWrite);
            executable.ExternalFileAttributes.HasFlag(ZipExtractor.ExecuteAttributes).Should().BeTrue();

            var normal = archive[1];
            normal.Name.Should().Be("normal");
            normal.IsFile.Should().BeTrue();
            normal.DateTime.Should().Be(TestFile.DefaultLastWrite);
            normal.ExternalFileAttributes.HasFlag(ZipExtractor.ExecuteAttributes).Should().BeFalse();

            var symlink = archive[2];
            symlink.Name.Should().Be("symlink");
            symlink.ExternalFileAttributes.HasFlag(ZipExtractor.SymlinkAttributes).Should().BeTrue();

            var directory = archive[3];
            directory.Name.Should().Be("dir/");
            directory.IsDirectory.Should().BeTrue();

            var sub = archive[4];
            sub.Name.Should().Be("dir/sub");
            sub.IsFile.Should().BeTrue();
            sub.DateTime.Should().Be(TestFile.DefaultLastWrite);
            sub.ExternalFileAttributes.HasFlag(ZipExtractor.ExecuteAttributes).Should().BeFalse();
        }
    }
}
