// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Text;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Tar;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Contains test methods for <see cref="TarBuilder"/>.
    /// </summary>
    public class TarBuilderTest : ArchiveBuilderTestBase
    {
        protected override IArchiveBuilder NewBuilder(Stream stream) => new TarBuilder(stream);

        protected override void AddElements(IForwardOnlyBuilder builder)
        {
            builder.AddFile("normal", TestFile.DefaultContents.ToStream(), TestFile.DefaultLastWrite);
            builder.AddFile("executable", TestFile.DefaultContents.ToStream(), TestFile.DefaultLastWrite, executable: true);
            builder.AddSymlink("symlink", target: "normal");
            builder.AddHardlink("hardlink", target: "normal");
            builder.AddDirectory("dir");
            builder.AddFile(Path.Combine("dir", "sub"), TestFile.DefaultContents.ToStream(), TestFile.DefaultLastWrite);
        }

        [Fact]
        public void Test()
        {
            var stream = GetArchiveStream();

            using var archive = new TarInputStream(stream, Encoding.UTF8);

            var normal = archive.GetNextEntry();
            normal.Name.Should().Be("normal");
            normal.ModTime.Should().Be(TestFile.DefaultLastWrite);
            normal.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode);

            var executable = archive.GetNextEntry();
            executable.Name.Should().Be("executable");
            executable.ModTime.Should().Be(TestFile.DefaultLastWrite);
            executable.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode | TarExtractor.ExecuteMode);

            var symlink = archive.GetNextEntry();
            symlink.Name.Should().Be("symlink");
            symlink.TarHeader.TypeFlag.Should().Be(TarHeader.LF_SYMLINK);

            var hardlink = archive.GetNextEntry();
            hardlink.Name.Should().Be("hardlink");
            hardlink.TarHeader.TypeFlag.Should().Be(TarHeader.LF_LINK);
            hardlink.TarHeader.LinkName.Should().Be("normal");

            var directory = archive.GetNextEntry();
            directory.Name.Should().Be("dir");
            directory.IsDirectory.Should().BeTrue();
            directory.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode | TarExtractor.ExecuteMode);

            var sub = archive.GetNextEntry();
            sub.Name.Should().Be("dir/sub");
            sub.ModTime.Should().Be(TestFile.DefaultLastWrite);
            sub.TarHeader.Mode.Should().Be(TarExtractor.DefaultMode);
        }
    }
}
