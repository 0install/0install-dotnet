// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

public class PrefixBuilderTest
{
    private const string Data = "data";
    private static Stream DataStream => Data.ToStream();
    private static readonly string _dataHash = Data.Hash(SHA1.Create());

    private readonly ManifestBuilder _innerBuilder = new(ManifestFormat.Sha1New);
    private readonly PrefixBuilder _builder;

    public PrefixBuilderTest()
    {
        _builder = new(_innerBuilder, "prefix");
    }

    [Fact]
    public void AddFile()
    {
        _builder.AddFile("file", DataStream, 1337);

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
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

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
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

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
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

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
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

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
            {
                ["file2"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void AddDirectory()
    {
        _builder.AddDirectory("dir");

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New) {"prefix/dir"});
    }

    [Fact]
    public void AddDirectoryAndFile()
    {
        // Implicit: _builder.AddDirectory("dir");
        _builder.AddFile(Path.Combine("dir", "file"), DataStream, 1337);

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix/dir"] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RenameDirectory()
    {
        _builder.AddDirectory("dir");
        _builder.AddFile(Path.Combine("dir", "file"), DataStream, 1337);
        _builder.Rename("dir", "dir2");

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix/dir2"] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void RemoveDirectory()
    {
        _builder.AddDirectory("dir");
        _builder.AddFile(Path.Combine("dir", "file"), DataStream, 1337);
        _builder.AddDirectory("dir2");
        _builder.AddFile(Path.Combine("dir2", "file"), DataStream, 1337);
        _builder.Remove("dir");

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix/dir2"] =
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

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
            {
                ["file"] = new ManifestNormalFile(_dataHash, 1337, 4),
                ["file2"] = new ManifestNormalFile(_dataHash, 1337, 4)
            }
        });
    }

    [Fact]
    public void AddSymlink()
    {
        _builder.AddSymlink("symlink", Data);

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
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

        _innerBuilder.Manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha1New)
        {
            ["prefix"] =
            {
                ["symlink"] = new ManifestSymlink(_dataHash, 4),
            }
        });
    }
}
