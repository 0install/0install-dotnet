// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Streams;
using NanoByte.Common.Values;
using ZeroInstall.Archives.Extractors;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Contains test methods for <see cref="ZipBuilder"/>.
/// </summary>
public class ZipBuilderTest : ArchiveBuilderTestBase
{
    protected override IArchiveBuilder NewBuilder(Stream stream) => new ZipBuilder(stream);

    protected override void AddElements(IForwardOnlyBuilder builder)
    {
        builder.AddFile("normal", TestFile.DefaultContents.ToStream(), TestFile.DefaultLastWrite);
        builder.AddFile("executable", TestFile.DefaultContents.ToStream(), TestFile.DefaultLastWrite, executable: true);
        builder.AddSymlink("symlink", target: "abc");
        builder.AddDirectory("dir");
        builder.AddFile(Path.Combine("dir", "sub"), TestFile.DefaultContents.ToStream(), TestFile.DefaultLastWrite);
    }

    [Fact]
    public void Test()
    {
        using var archive = new ZipFile(GetArchiveStream());

        var normal = archive[0];
        normal.Name.Should().Be("normal");
        normal.IsFile.Should().BeTrue();
        normal.DateTime.Should().Be(TestFile.DefaultLastWrite);
        normal.ExternalFileAttributes.HasFlag(ZipExtractor.ExecuteAttributes).Should().BeFalse();

        var executable = archive[1];
        executable.Name.Should().Be("executable");
        executable.IsFile.Should().BeTrue();
        executable.DateTime.Should().Be(TestFile.DefaultLastWrite);
        executable.ExternalFileAttributes.HasFlag(ZipExtractor.ExecuteAttributes).Should().BeTrue();

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
