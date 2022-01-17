// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Streams;
using Xunit;

namespace ZeroInstall.Store.Manifests;

public class ManifestBuilderTest
{
    private const string Data = "data";
    private static Stream DataStream => Data.ToStream();
    private static readonly string _dataHash = Data.Hash(SHA1.Create());

    private readonly ManifestBuilder _builder = new(ManifestFormat.Sha1New);

    [Fact]
    public void AddFile()
    {
        _builder.AddFile("file", DataStream, 1337);

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void OverwriteFile()
    {
        _builder.AddFile("file", "dummy".ToStream(), 42);
        _builder.AddFile("file", DataStream, 1337, executable: true);

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["file"] = new ManifestExecutableFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void MarkAsExecutable()
    {
        _builder.AddFile("file", DataStream, 1337);
        _builder.MarkAsExecutable("file");

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["file"] = new ManifestExecutableFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RenameFile()
    {
        _builder.AddFile("file", DataStream, 1337, executable: true);
        _builder.Rename("file", "file2");

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["file2"] = new ManifestExecutableFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RenameFileMissing()
    {
        _builder.Invoking(x => x.Rename("file", "file2"))
                .Should().Throw<IOException>();
    }

    [Fact]
    public void RemoveFile()
    {
        _builder.AddFile("file", DataStream, 1337);
        _builder.AddFile("file2", DataStream, 1337);
        _builder.Remove("file");

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["file2"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RemoveFileMissing()
    {
        _builder.Invoking(x => x.Remove("file"))
                .Should().Throw<IOException>();
    }

    [Fact]
    public void AddDirectory()
    {
        _builder.AddDirectory("dir");

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New) {"dir"});
    }

    [Fact]
    public void AddDirectoryAndFile()
    {
        // Implicit: _builder.AddDirectory("dir");
        // Implicit: _builder.AddDirectory("subdir1");
        _builder.AddFile(Path.Combine("dir", "subdir1", "file"), DataStream, 1337);

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            // ReSharper disable once RedundantEmptyObjectOrCollectionInitializer
            ["dir"] = {},
            ["dir/subdir1"] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RenameDirectory()
    {
        _builder.AddDirectory(Path.Combine("dir", "sub1"));
        _builder.AddFile(Path.Combine("dir", "sub1", "file"), DataStream, 1337);
        _builder.Rename(Path.Combine("dir", "sub1"), Path.Combine("dir", "sub2"));

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["dir/sub2"] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RemoveDirectory()
    {
        _builder.AddDirectory(Path.Combine("dir1", "sub"));
        _builder.AddFile(Path.Combine("dir1", "sub", "file"), DataStream, 1337);
        _builder.AddDirectory("dir2");
        _builder.AddFile(Path.Combine("dir2", "file"), DataStream, 1337);
        _builder.Remove(Path.Combine("dir1"));

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["dir2"] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void AddHardLink()
    {
        _builder.AddFile("file", DataStream, 1337);
        _builder.AddHardlink("file2", "file");

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4),
                ["file2"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void AddHardLinkMissing()
    {
        _builder.Invoking(x => x.AddHardlink("file2", "file"))
                .Should().Throw<IOException>();
    }

    [Fact]
    public void AddSymlink()
    {
        _builder.AddSymlink("symlink", Data);

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["symlink"] = new ManifestSymlink(_dataHash, 4),
            }
        });
    }

    [Fact]
    public void TurnIntoSymlink()
    {
        _builder.AddFile("symlink", DataStream, 1337);
        _builder.TurnIntoSymlink("symlink");

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            [""] =
            {
                ["symlink"] = new ManifestSymlink(_dataHash, 4),
            }
        });
    }

    [Fact]
    public void Complex()
    {
        _builder.AddDirectory(Path.Combine("some", "dir"));
        _builder.AddFile(Path.Combine("some", "dir", "file"), DataStream, 1337);
        _builder.Rename(Path.Combine("some", "dir", "file"), Path.Combine("some", "dir", "file1"));
        _builder.AddHardlink(Path.Combine("some", "dir", "file2"), Path.Combine("some", "dir", "file1"));
        _builder.Rename("some", "the");

        _builder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["the/dir"] =
            {
                ["file1"] = new ManifestNormalFile(_dataHash, 1337, 4),
                ["file2"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RejectsInvalidPaths()
    {
        _builder.Invoking(x => x.AddFile("a\nb", DataStream, 0))
                .Should().Throw<IOException>();
        _builder.Invoking(x => x.AddFile(".manifest", DataStream, 0))
                .Should().Throw<IOException>();
        _builder.Invoking(x => x.AddFile(".xbit", DataStream, 0))
                .Should().Throw<IOException>();
    }
}
