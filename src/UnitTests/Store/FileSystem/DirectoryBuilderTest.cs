// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

public class DirectoryBuilderTest : IDisposable
{
    private const string Data = "data";
    private static Stream DataStream => Data.ToStream();

    private readonly TemporaryDirectory _tempDir = new("0install-unit-test-impl");
    private readonly DirectoryBuilder _builder;

    public DirectoryBuilderTest()
    {
        _builder = new DirectoryBuilder(_tempDir, new ManifestBuilder(ManifestFormat.Sha1New));
    }

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    public void AddFile()
    {
        _builder.AddFile("file", DataStream, modifiedTime: 1337);

        Verify([
            new TestFile("file") { Contents = Data, LastWrite = 1337 }
        ]);
    }

    [Fact]
    public void OverwriteFile()
    {
        _builder.AddFile("file", "dummy".ToStream(), modifiedTime: 42);
        _builder.AddFile("file", DataStream, modifiedTime: 1337, executable: true);

        Verify([
            new TestFile("file") { Contents = Data, LastWrite = 1337, IsExecutable = true }
        ]);
    }

    [Fact]
    public void MarkAsExecutable()
    {
        _builder.AddFile("file", DataStream, modifiedTime: 1337);
        _builder.MarkAsExecutable("file");

        Verify([
            new TestFile("file") { Contents = Data, LastWrite = 1337, IsExecutable = true }
        ]);
    }

    [Fact]
    public void RenameFile()
    {
        _builder.AddFile("file", DataStream, modifiedTime: 1337, executable: true);
        _builder.Rename("file", "file2");

        Verify([
            new TestFile("file2") { Contents = Data, LastWrite = 1337, IsExecutable = true }
        ]);
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
        _builder.AddFile("file", DataStream, modifiedTime: 1337);
        _builder.AddFile("file2", DataStream, modifiedTime: 2000);
        _builder.Remove("file");

        Verify([
            new TestFile("file2") { Contents = Data, LastWrite = 2000 }
        ]);
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

        Verify([new TestDirectory("dir")]);
    }

    [Fact]
    public void AddDirectoryAndFile()
    {
        // Implicit: _builder.AddDirectory("dir");
        _builder.AddFile(Path.Combine("dir", "file"), DataStream, modifiedTime: 1337);

        Verify([
            new TestDirectory("dir")
            {
                new TestFile("file") { Contents = Data, LastWrite = 1337 }
            }
        ]);
    }

    [Fact]
    public void RenameDirectory()
    {
        _builder.AddDirectory("dir");
        _builder.AddFile(Path.Combine("dir", "file"), DataStream, modifiedTime: 1337);
        _builder.Rename("dir", "dir2");

        Verify([
            new TestDirectory("dir2")
            {
                new TestFile("file") { Contents = Data, LastWrite = 1337 }
            }
        ]);
    }

    [Fact]
    public void RemoveDirectory()
    {
        _builder.AddDirectory("dir");
        _builder.AddFile(Path.Combine("dir", "file"), DataStream, modifiedTime: 1337);
        _builder.AddDirectory("dir2");
        _builder.AddFile(Path.Combine("dir2", "file"), DataStream, modifiedTime: 2000);
        _builder.Remove("dir");

        Verify([
            new TestDirectory("dir2")
            {
                new TestFile("file") { Contents = Data, LastWrite = 2000 }
            }
        ]);
    }

    [Fact]
    public void AddHardLink()
    {
        _builder.AddFile(Path.Combine("dir", "file"), DataStream, modifiedTime: 1337);
        _builder.AddHardlink(Path.Combine("dir", "file2"), Path.Combine("dir", "file"));

        Verify([
            new TestDirectory("dir")
            {
                new TestFile("file") { Contents = Data, LastWrite = 1337 },
                new TestFile("file2") { Contents = Data, LastWrite = 1337 }
            }
        ]);
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
        _builder.AddSymlink(Path.Combine("dir", "symlink"), "target");

        Verify([
            new TestDirectory("dir")
            {
                new TestSymlink("symlink", "target")
            }
        ]);
    }

    [Fact]
    public void TurnIntoSymlink()
    {
        _builder.AddFile(Path.Combine("dir", "symlink"), "target".ToStream(), modifiedTime: 0);
        _builder.TurnIntoSymlink(Path.Combine("dir", "symlink"));

        Verify([
            new TestDirectory("dir")
            {
                new TestSymlink("symlink", "target")
            }
        ]);
    }

    [Fact]
    public void Complex()
    {
        _builder.AddDirectory(Path.Combine("some", "dir"));
        _builder.AddFile(Path.Combine("some", "dir", "file"), DataStream, modifiedTime: 1337);
        _builder.Rename(Path.Combine("some", "dir", "file"), Path.Combine("some", "dir", "file1"));
        _builder.AddHardlink(Path.Combine("some", "dir", "file2"), Path.Combine("some", "dir", "file1"));
        _builder.Rename("some", "the");

        Verify([
            new TestDirectory("the")
            {
                new TestDirectory("dir")
                {
                    new TestFile("file1") { Contents = Data, LastWrite = 1337 },
                    new TestFile("file2") { Contents = Data, LastWrite = 1337 }
                }
            }
        ]);
    }

    [Fact]
    public void RejectsInvalidPaths()
    {
        _builder.Invoking(x => x.AddFile("a\nb", DataStream, modifiedTime: 0))
                .Should().Throw<IOException>();
        _builder.Invoking(x => x.AddFile(".manifest", DataStream, modifiedTime: 0))
                .Should().Throw<IOException>();
        _builder.Invoking(x => x.AddFile(".xbit", DataStream, modifiedTime: 0))
                .Should().Throw<IOException>();
    }

    private void Verify(TestRoot directory)
        => directory.Verify(_tempDir);
}
