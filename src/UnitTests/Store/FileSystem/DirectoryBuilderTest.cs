// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

public class DirectoryBuilderTest : IDisposable
{
    private const string Data = "data";
    private static Stream DataStream => Data.ToStream();

    private readonly TemporaryDirectory _tempDir = new("0install-unit-test-impl");
    private readonly ManifestBuilder _manifestBuilder = new(ManifestFormat.Sha1New);
    private readonly DirectoryBuilder _builder;

    public DirectoryBuilderTest()
    {
        _builder = new DirectoryBuilder(_tempDir, _manifestBuilder);
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
    public void SkipsAppleDoubleFileAfterOriginal()
    {
        Assert.SkipWhen(UnixUtils.IsMacOSX, "AppleDouble files are kept on macOS");

        _builder.AddFile("file", DataStream, modifiedTime: 1337);
        var appleDouble = "metadata".ToStream();
        _builder.AddFile("._file", appleDouble, modifiedTime: 1337);

        appleDouble.Position.Should().Be(appleDouble.Length, because: "the content should be consumed");
        Verify([
            new TestFile("file") { Contents = Data, LastWrite = 1337 }
        ]);
        File.Exists(Path.Combine(_tempDir, "._file")).Should().BeFalse();
        _manifestBuilder.Manifest[""].Keys.Should().Equal("file");
    }

    [Fact]
    public void DeletesAppleDoubleFileBeforeOriginal()
    {
        Assert.SkipWhen(UnixUtils.IsMacOSX, "AppleDouble files are kept on macOS");

        _builder.AddFile("._file", "metadata".ToStream(), modifiedTime: 1337);
        _builder.AddFile("file", DataStream, modifiedTime: 1337);

        Verify([
            new TestFile("file") { Contents = Data, LastWrite = 1337 }
        ]);
        File.Exists(Path.Combine(_tempDir, "._file")).Should().BeFalse();
        _manifestBuilder.Manifest[""].Keys.Should().Equal("file");
    }

    [Fact]
    public void KeepsAppleDoubleFileOnMacOS()
    {
        Assert.SkipUnless(UnixUtils.IsMacOSX, "AppleDouble files are only kept on macOS");

        _builder.AddFile("file", DataStream, modifiedTime: 1337);
        _builder.AddFile("._file", "metadata".ToStream(), modifiedTime: 1337);

        File.Exists(Path.Combine(_tempDir, "._file")).Should().BeTrue();
        _manifestBuilder.Manifest[""].Keys.Should().Equal("file");
    }

    [Fact]
    public void KeepsFileNamedLikeAppleDoubleForDirectory()
    {
        _builder.AddDirectory("file");
        _builder.AddFile("._file", "metadata".ToStream(), modifiedTime: 1337);

        Verify([
            new TestFile("._file") { Contents = "metadata", LastWrite = 1337 },
            new TestDirectory("file")
        ]);
        _manifestBuilder.Manifest[""].Keys.Should().Equal("._file");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KeepsFileNamedLikeAppleDoubleWithDifferentCase(bool forcePortable)
    {
        var builder = CreateBuilder(forcePortable);

        builder.AddFile("file", DataStream, modifiedTime: 1337);
        builder.AddFile("._FILE", "metadata".ToStream(), modifiedTime: 1337);

        Directory.GetFiles(_tempDir).Select(Path.GetFileName).Should().BeEquivalentTo("file", "._FILE");
        _manifestBuilder.Manifest[""].Keys.Should().BeEquivalentTo("file", "._FILE");
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

    [Theory]
    [InlineData(@"..\outside")]
    [InlineData("../../outside")]
    [InlineData(@"dir\..\..\outside")]
    [InlineData(@"C:\Windows")]
    [InlineData("C:foo")]
    [InlineData(@"\foo")]
    [InlineData(@"\\server\share")]
    [InlineData("target:stream")]
    public void AddSymlinkOutsideImplementationOnWindows(string target)
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Real symlinks are only avoided on Windows");

        _builder.AddSymlink("symlink", target);

        string path = Path.Combine(_tempDir, "symlink");
        FileUtils.IsSymlink(path).Should().BeFalse(because: "symlinks pointing outside of the implementation should not be real NTFS symlinks");
        ImplFileUtils.IsSymlink(path, out string? actualTarget).Should().BeTrue();
        actualTarget.Should().Be(target);
    }

    [Theory]
    [InlineData("target")]
    [InlineData("dir/../target")]
    [InlineData(".")]
    public void AddSymlinkInsideImplementation(string target)
    {
        _builder.AddSymlink("symlink", target);

        ImplFileUtils.IsSymlink(Path.Combine(_tempDir, "symlink"), out string? actualTarget).Should().BeTrue();
        actualTarget.Should().Be(target);
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

    [Theory]
    [InlineData("")]
    [InlineData("target\0evil")]
    [InlineData("target\nevil")]
    public void TurnIntoSymlinkRejectsInvalidTarget(string target)
    {
        _builder.AddFile("symlink", target.ToStream(), modifiedTime: 0);

        _builder.Invoking(x => x.TurnIntoSymlink("symlink"))
                .Should().Throw<IOException>();
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

    [Fact]
    public void CopyFromTopLevel()
    {
        using var sourceDir = Build([
            new TestDirectory("subdir")
            {
                new TestFile("file") { Contents = Data, LastWrite = 1337 }
            },
            new TestFile("root-file") { Contents = Data, LastWrite = 1337 }
        ]);

        var metadata = new CopyFromStep();
        _builder.CopyFrom(metadata, sourceDir, new SilentTaskHandler());

        Verify([
            new TestDirectory("subdir")
            {
                new TestFile("file") { Contents = Data, LastWrite = 1337 }
            },

            new TestFile("root-file") { Contents = Data, LastWrite = 1337 }
        ]);
    }

    [Fact]
    public void CopyFromFile()
    {
        using var sourceDir = Build([
            new TestFile("source-file") { Contents = Data, LastWrite = 1337, IsExecutable = true }
        ]);

        var metadata = new CopyFromStep { Source = "source-file", Destination = "dest-file" };
        _builder.CopyFrom(metadata, sourceDir, new SilentTaskHandler());

        Verify([new TestFile("dest-file") { Contents = Data, LastWrite = 1337, IsExecutable = true }]);
    }

    [Fact]
    public void CopyFromFileImplicitDestination()
    {
        using var sourceDir = Build([
            new TestFile("source-file") { Contents = Data, LastWrite = 1337, IsExecutable = true }
        ]);

        var metadata = new CopyFromStep { Source = "source-file" };
        _builder.CopyFrom(metadata, sourceDir, new SilentTaskHandler());

        Verify([new TestFile("source-file") { Contents = Data, LastWrite = 1337, IsExecutable = true }]);
    }

    [Fact]
    public void CopyFromFileInSubDir()
    {
        using var sourceDir = Build([
            new TestDirectory("subdir")
            {
                new TestFile("source-file") { Contents = Data, LastWrite = 1337, IsExecutable = true }
            }
        ]);

        var metadata = new CopyFromStep { Source = "subdir/source-file", Destination = "dest-file" };
        _builder.CopyFrom(metadata, sourceDir, new SilentTaskHandler());

        Verify([new TestFile("dest-file") { Contents = Data, LastWrite = 1337, IsExecutable = true }]);
    }

    [Fact]
    public void CopyFromDirectory()
    {
        using var sourceDir = Build([
            new TestDirectory("source-dir")
            {
                new TestFile("file1") { Contents = Data, LastWrite = 1337 },
                new TestFile("file2") { Contents = "more data", LastWrite = 2000, IsExecutable = true }
            }
        ]);

        var metadata = new CopyFromStep {Source = "source-dir", Destination = "dest-dir"};
        _builder.CopyFrom(metadata, sourceDir, new SilentTaskHandler());

        Verify([
            new TestDirectory("dest-dir")
            {
                new TestFile("file1") { Contents = Data, LastWrite = 1337 },
                new TestFile("file2") { Contents = "more data", LastWrite = 2000, IsExecutable = true }
            }
        ]);
    }

    [Fact]
    public void TryAddExternalHardlink_WithinAllowedRoot()
    {
        string sourceFile = Path.Combine(_tempDir, "source-file");
        File.WriteAllText(sourceFile, Data);

        _builder.TryAddExternalHardlink("dest-file", new FileInfo(sourceFile))
                .Should().BeTrue();
        FileUtils.AreHardlinked(sourceFile, Path.Combine(_tempDir, "dest-file"))
                 .Should().BeTrue();
    }

    [Fact]
    public void TryAddExternalHardlink_OutsideAllowedRoot()
    {
        using var outsideDir = new TemporaryDirectory("0install-unit-test-outside");
        string sourceFile = Path.Combine(outsideDir, "file");
        File.WriteAllText(sourceFile, Data);

        _builder.TryAddExternalHardlink("dest-file", new FileInfo(sourceFile))
                .Should().BeFalse();
    }

    [Fact]
    public void CopyFromFile_UsesHardlink()
    {
        using var root = new TemporaryDirectory("0install-unit-test-root");
        string sourceDir = Directory.CreateDirectory(Path.Combine(root, "source")).FullName;
        string destDir = Directory.CreateDirectory(Path.Combine(root, "dest")).FullName;
        new TestRoot {
            new TestFile("file") { Contents = Data, LastWrite = 1337 }
        }.Build(sourceDir);

        var builder = new DirectoryBuilder(destDir, new ManifestBuilder(ManifestFormat.Sha1New)) { AllowedHardlinkRoot = root };
        builder.CopyFrom(new CopyFromStep { Source = "file" }, sourceDir, new SilentTaskHandler());

        FileUtils.AreHardlinked(
            Path.Combine(sourceDir, "file"),
            Path.Combine(destDir, "file")).Should().BeTrue();
    }

    [Fact]
    public void CopyFromDirectory_UsesHardlinks()
    {
        using var root = new TemporaryDirectory("0install-unit-test-root");
        string sourceDir = Directory.CreateDirectory(Path.Combine(root, "source")).FullName;
        string destDir = Directory.CreateDirectory(Path.Combine(root, "dest")).FullName;
        new TestRoot
        {
            new TestFile("file1") { Contents = Data, LastWrite = 1337 },
            new TestFile("file2") { Contents = "more data", LastWrite = 2000 }
        }.Build(sourceDir);

        var builder = new DirectoryBuilder(destDir, new ManifestBuilder(ManifestFormat.Sha1New)) { AllowedHardlinkRoot = root };
        builder.CopyFrom(new CopyFromStep(), sourceDir, new SilentTaskHandler());

        FileUtils.AreHardlinked(Path.Combine(sourceDir, "file1"), Path.Combine(destDir, "file1")).Should().BeTrue();
        FileUtils.AreHardlinked(Path.Combine(sourceDir, "file2"), Path.Combine(destDir, "file2")).Should().BeTrue();
    }

    [Fact]
    public void RejectsAlternateDataStreams()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Alternate data streams are Windows-specific");

        _builder.AddFile("file", DataStream, modifiedTime: 0);
        _builder.Invoking(x => x.AddFile("file:stream", DataStream, modifiedTime: 0))
                .Should().Throw<IOException>();
        _builder.Invoking(x => x.AddFile("file::$DATA", DataStream, modifiedTime: 0))
                .Should().Throw<IOException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsCaseCollisions(bool forcePortable)
    {
        Assert.SkipUnless(forcePortable || WindowsUtils.IsWindows, "Only Windows is known to resolve names case-insensitively on all test machines");
        var builder = CreateBuilder(forcePortable);

        builder.AddDirectory("dir");
        builder.AddFile(Path.Combine("dir", "file"), DataStream, modifiedTime: 1337);

        builder.Invoking(x => x.AddDirectory("DIR")).Should().Throw<IOException>();
        builder.Invoking(x => x.AddFile(Path.Combine("DIR", "new"), DataStream, modifiedTime: 0)).Should().Throw<IOException>();
        builder.Invoking(x => x.AddFile(Path.Combine("dir", "FILE"), "other".ToStream(), modifiedTime: 0)).Should().Throw<IOException>();
        builder.Invoking(x => x.AddSymlink(Path.Combine("dir", "FILE"), "target")).Should().Throw<IOException>();
        builder.Invoking(x => x.AddHardlink(Path.Combine("dir", "FILE"), Path.Combine("dir", "file"))).Should().Throw<IOException>();
        builder.Invoking(x => x.AddHardlink("link", Path.Combine("dir", "FILE"))).Should().Throw<IOException>();
        builder.Invoking(x => x.Rename("DIR", "dir2")).Should().Throw<IOException>();
        builder.Invoking(x => x.Rename(Path.Combine("dir", "file"), Path.Combine("dir", "FILE"))).Should().Throw<IOException>();
        builder.Invoking(x => x.Remove(Path.Combine("dir", "FILE"))).Should().Throw<IOException>();
        builder.Invoking(x => x.MarkAsExecutable(Path.Combine("dir", "FILE"))).Should().Throw<IOException>();
        builder.Invoking(x => x.TurnIntoSymlink(Path.Combine("dir", "FILE"))).Should().Throw<IOException>();

        Verify([
            new TestDirectory("dir")
            {
                new TestFile("file") { Contents = Data, LastWrite = 1337 }
            }
        ]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsShortNameAliases(bool forcePortable)
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "8.3 short names are Windows-specific");
        var builder = CreateBuilder(forcePortable);

        builder.AddFile("longfilename.txt", DataStream, modifiedTime: 1337);
        Assert.SkipUnless(File.Exists(Path.Combine(_tempDir, "LONGFI~1.TXT")), "8.3 short name generation is disabled on this volume");

        builder.Invoking(x => x.AddFile("LONGFI~1.TXT", "other".ToStream(), modifiedTime: 0)).Should().Throw<IOException>();
        builder.Invoking(x => x.Remove("LONGFI~1.TXT")).Should().Throw<IOException>();

        Verify([
            new TestFile("longfilename.txt") { Contents = Data, LastWrite = 1337 }
        ]);
    }

    [Theory]
    [InlineData("t", "T", false)]
    [InlineData("t", "T", true)]
    [InlineData("é", "é", false)]
    [InlineData("é", "é", true)]
    public void DiskMatchesManifestDespiteAliases(string name, string alias, bool forcePortable)
    {
        var builder = CreateBuilder(forcePortable);

        // Would leave "evil" in A/f on disk while the manifest says "good" if the file system resolved the alias to the same entry
        var rejected = new List<int>();
        Action[] steps =
        [
            () => builder.AddDirectory(name),
            () => builder.AddDirectory(alias),
            () => builder.AddFile(Path.Combine(alias, "f"), "good".ToStream(), modifiedTime: 0),
            () => builder.AddFile(Path.Combine(name, "f"), "evil".ToStream(), modifiedTime: 0),
            () => builder.Rename(alias, "A"),
            () => builder.AddDirectory(name),
            () => builder.Remove(name)
        ];
        for (int i = 0; i < steps.Length; i++)
        {
            try
            {
                steps[i]();
            }
            catch (IOException)
            {
                rejected.Add(i);
            }
        }

        if (forcePortable) rejected.Should().Contain(1, because: "the alias of an existing entry should be rejected");
        var diskManifest = new ManifestBuilder(ManifestFormat.Sha1New);
        new ReadDirectory(_tempDir, diskManifest).Run(TestContext.Current.CancellationToken);
        diskManifest.Manifest.ToString().Should().Be(_manifestBuilder.Manifest.ToString());
    }

    [Fact]
    public void ListingCacheStaysCorrect()
    {
        var builder = CreateBuilder(forcePortable: true);

        builder.AddFile(Path.Combine("dir", "a"), DataStream, modifiedTime: 1337);
        builder.Rename(Path.Combine("dir", "a"), Path.Combine("dir", "b"));
        builder.AddFile(Path.Combine("dir", "A"), DataStream, modifiedTime: 1337); // "a" no longer exists
        builder.Invoking(x => x.AddFile(Path.Combine("dir", "B"), DataStream, modifiedTime: 0)).Should().Throw<IOException>();

        builder.Remove("dir");
        builder.AddFile(Path.Combine("DIR", "b"), DataStream, modifiedTime: 1337); // "dir" no longer exists
        builder.Invoking(x => x.AddFile(Path.Combine("dir", "c"), DataStream, modifiedTime: 0)).Should().Throw<IOException>();
        builder.Invoking(x => x.AddFile(Path.Combine("DIR", "B"), DataStream, modifiedTime: 0)).Should().Throw<IOException>();

        Verify([
            new TestDirectory("DIR")
            {
                new TestFile("b") { Contents = Data, LastWrite = 1337 }
            }
        ]);
    }

    [Fact]
    public void DoesNotFollowDirectoryLinks()
    {
        using var outsideDir = new TemporaryDirectory("0install-unit-test-outside");
        File.WriteAllText(Path.Combine(outsideDir, "victim"), Data);
        CreateDirectoryLink(Path.Combine(_tempDir, "link"), outsideDir);

        string Inside(string name) => Path.Combine("link", name);

        _builder.Invoking(x => x.AddFile(Inside("new"), DataStream, modifiedTime: 0)).Should().Throw<IOException>();
        _builder.Invoking(x => x.AddFile(Inside("victim"), DataStream, modifiedTime: 0)).Should().Throw<IOException>();
        _builder.Invoking(x => x.AddDirectory(Inside("dir"))).Should().Throw<IOException>();
        _builder.Invoking(x => x.AddSymlink(Inside("symlink"), "target")).Should().Throw<IOException>();
        _builder.Invoking(x => x.Remove(Inside("victim"))).Should().Throw<IOException>();
        _builder.Invoking(x => x.Rename(Inside("victim"), "stolen")).Should().Throw<IOException>();
        _builder.Invoking(x => x.MarkAsExecutable(Inside("victim"))).Should().Throw<IOException>();
        _builder.Invoking(x => x.TurnIntoSymlink(Inside("victim"))).Should().Throw<IOException>();
        _builder.Invoking(x => x.AddHardlink("hardlink", Inside("victim"))).Should().Throw<IOException>();
        _builder.AddFile("file", DataStream, modifiedTime: 0);
        _builder.Invoking(x => x.Rename("file", Inside("planted"))).Should().Throw<IOException>();
        _builder.Invoking(x => x.AddHardlink(Inside("planted"), "file")).Should().Throw<IOException>();

        Directory.GetFileSystemEntries(outsideDir).Should().Equal(Path.Combine(outsideDir, "victim"));
        File.ReadAllText(Path.Combine(outsideDir, "victim")).Should().Be(Data);
        ImplFileUtils.IsExecutable(Path.Combine(outsideDir, "victim")).Should().BeFalse();
    }

    [Fact]
    public void RemovesDirectoryLinkWithoutFollowing()
    {
        using var outsideDir = new TemporaryDirectory("0install-unit-test-outside");
        File.WriteAllText(Path.Combine(outsideDir, "victim"), Data);
        CreateDirectoryLink(Path.Combine(_tempDir, "link"), outsideDir);

        new DirectoryBuilder(_tempDir).Remove("link");

        Directory.Exists(Path.Combine(_tempDir, "link")).Should().BeFalse();
        File.ReadAllText(Path.Combine(outsideDir, "victim")).Should().Be(Data);
    }

    /// <summary>
    /// Creates a directory junction (Windows) or symlink (Unix). Simulates a link planted by a previous build step.
    /// </summary>
    private static void CreateDirectoryLink(string link, string target)
    {
        if (WindowsUtils.IsWindows)
        {
            var process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"") {UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true})!;
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.ExitCode.Should().Be(0);
        }
        else FileUtils.CreateSymlink(link, target);
    }

    /// <summary>
    /// Creates a builder for <see cref="_tempDir"/> that feeds <see cref="_manifestBuilder"/>.
    /// </summary>
    /// <param name="forcePortable">Enable exact-name checks using the portable lookup, even if the file system resolves names exactly.</param>
    private DirectoryBuilder CreateBuilder(bool forcePortable)
        => forcePortable ? new(_tempDir, _manifestBuilder) { ForcePortableNameChecks = true } : _builder;

    [MustUseReturnValue]
    private static TemporaryDirectory Build(TestRoot directory)
    {
        var dir = new TemporaryDirectory("0install-unit-test-source");
        directory.Build(dir);
        return dir;
    }

    private void Verify(TestRoot directory)
        => directory.Verify(_tempDir);
}
