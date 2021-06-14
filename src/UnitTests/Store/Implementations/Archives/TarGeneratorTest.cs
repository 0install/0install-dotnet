// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Text;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Tar;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Contains test methods for <see cref="TarGenerator"/>.
    /// </summary>
    public class TarGeneratorTest : ArchiveGeneratorTest<TarGenerator>
    {
        protected override TarGenerator CreateGenerator(string sourceDirectory, Stream stream) => new(sourceDirectory, stream);

        [Fact]
        public void FileOrder()
        {
            var stream = BuildArchive(new TestRoot {new TestFile("x"), new TestFile("y"), new TestFile("Z")});

            using var archive = new TarInputStream(stream, Encoding.UTF8);
            archive.GetNextEntry().Name.Should().Be("Z");
            archive.GetNextEntry().Name.Should().Be("x");
            archive.GetNextEntry().Name.Should().Be("y");
        }

        [Fact]
        public void FileTypes()
        {
            var stream = BuildArchive(new TestRoot
            {
                new TestFile("executable") {IsExecutable = true},
                new TestFile("normal"),
                new TestSymlink("symlink", target: "abc"),
                new TestDirectory("dir") {new TestFile("sub")}
            });

            using var archive = new TarInputStream(stream, Encoding.UTF8);
            var executable = archive.GetNextEntry();
            executable.Name.Should().Be("executable");
            executable.ModTime.Should().Be(TestFile.DefaultLastWrite);
            executable.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode | TarExtractor.ExecuteMode);

            var normal = archive.GetNextEntry();
            normal.Name.Should().Be("normal");
            normal.ModTime.Should().Be(TestFile.DefaultLastWrite);
            normal.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode);

            var symlink = archive.GetNextEntry();
            symlink.Name.Should().Be("symlink");
            symlink.TarHeader.TypeFlag.Should().Be(TarHeader.LF_SYMLINK);

            var directory = archive.GetNextEntry();
            directory.Name.Should().Be("dir");
            directory.IsDirectory.Should().BeTrue();
            directory.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode | TarExtractor.ExecuteMode);

            var sub = archive.GetNextEntry();
            sub.Name.Should().Be("dir/sub");
            sub.ModTime.Should().Be(TestFile.DefaultLastWrite);
            sub.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode);
        }

        [Fact]
        public void Hardlink()
        {
            Stream stream;
            using (var tempDir = new TemporaryDirectory("0install-test-archives"))
            {
                new TestRoot {new TestFile("file")}.Build(tempDir);
                FileUtils.CreateHardlink(
                    sourcePath: Path.Combine(tempDir, "hardlink"),
                    targetPath: Path.Combine(tempDir, "file"));
                stream = BuildArchive(tempDir);
            }

            using var archive = new TarInputStream(stream, Encoding.UTF8);
            var file = archive.GetNextEntry();
            file.Name.Should().Be("file");

            var hardlink = archive.GetNextEntry();
            hardlink.Name.Should().Be("hardlink");
            hardlink.TarHeader.TypeFlag.Should().Be(TarHeader.LF_LINK);
            hardlink.TarHeader.LinkName.Should().Be("file");
        }
    }
}
