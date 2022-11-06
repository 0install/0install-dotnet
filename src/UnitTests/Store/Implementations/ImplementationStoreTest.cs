// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.FileSystem;
using ZeroInstall.Services;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Contains test methods for <see cref="ImplementationStore"/>.
/// </summary>
public class ImplementationStoreTest : IDisposable
{
    private readonly MockTaskHandler _handler = new();
    private readonly TemporaryDirectory _tempDir = new("0install-test-store");
    private readonly ImplementationStore _store;

    public ImplementationStoreTest()
    {
        _store = new ImplementationStore(_tempDir, _handler);
    }

    public void Dispose()
    {
        _store.Purge();
        _tempDir.Dispose();
    }

    [Fact]
    public void Contains()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "sha256new_123ABC"));
        _store.Contains(new ManifestDigest(Sha256New: "123ABC")).Should().BeTrue();
        _store.Contains(new ManifestDigest(Sha256New: "456XYZ")).Should().BeFalse();
    }

    [Fact]
    public void ListAll()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "sha1=test1"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "sha1new=test2"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "sha256=test3"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "sha256new_test4"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "temp=stuff"));
        _store.ListAll().Should().BeEquivalentTo(new ManifestDigest[]
        {
            new(Sha1: "test1"),
            new(Sha1New: "test2"),
            new(Sha256: "test3"),
            new(Sha256New: "test4")
        });
    }

    [Fact]
    public void AddEmptyDirectory()
    {
        _store.Add(ManifestDigest.Empty, _ => {});
        _store.Contains(ManifestDigest.Empty).Should().BeTrue();
        _store.ListAll().Should().Equal(new ManifestDigest(ManifestDigest.Empty.Best!));
    }

    [Fact]
    public void RecreateMissingStoreDir()
    {
        Directory.Delete(_tempDir, recursive: true);

        AddEmptyDirectory();
    }

    [Fact]
    public void ShouldAllowToRemove()
    {
        string implPath = Path.Combine(_tempDir, "sha256new_123ABC");
        Directory.CreateDirectory(implPath);

        _store.Remove(new ManifestDigest(Sha256New: "123ABC")).Should().BeTrue();
        Directory.Exists(implPath).Should().BeFalse();
    }

    [Fact]
    public void ShouldAllowToPurge()
    {
        string implPath1 = Path.Combine(_tempDir, "sha256new_123ABC");
        string implPath2 = Path.Combine(_tempDir, "sha256new_456XYZ");
        string extractPath = Path.Combine(_tempDir, "0install-extract-abc");
        using var otherPath = new TemporaryDirectory("other", _tempDir);
        Directory.CreateDirectory(implPath1);
        Directory.CreateDirectory(implPath2);
        Directory.CreateDirectory(extractPath);
        Directory.CreateDirectory(otherPath);

        _store.Purge();
        Directory.Exists(implPath1).Should().BeFalse();
        Directory.Exists(implPath2).Should().BeFalse();
        Directory.Exists(extractPath).Should().BeFalse();
        Directory.Exists(otherPath).Should().BeTrue();
    }

    [Fact]
    public void GetPath()
    {
        string implPath = Path.Combine(_tempDir, "sha256new_123ABC");
        Directory.CreateDirectory(implPath);
        _store.GetPath(new ManifestDigest(Sha256New: "123ABC"))
              .Should().Be(implPath, because: "Store must return the correct path for Implementations it contains");
    }

    [Fact]
    public void GetPathMissingImplementation()
        => _store.GetPath(new ManifestDigest(Sha256: "123"))
                 .Should().BeNull();

    private static readonly ManifestDigest _referenceDigest = new(Sha256New: "DIXH3X4A5UJ537O2B36IYYVNRO2MYJVJYX74GBF4EOY5CDCCWGQA");

    [Fact]
    public void Verify()
    {
        _store.Add(_referenceDigest, builder => builder.AddFile("file", "AAA".ToStream(), 0));

        _store.Verify(_referenceDigest);
        _handler.LastQuestion.Should().BeNull();
    }

    [Fact]
    public void VerifyDespiteTimestampOffset()
    {
        _store.Add(_referenceDigest, builder => builder.AddFile("file", "AAA".ToStream(), 3600));

        _store.Verify(_referenceDigest);
        _handler.LastQuestion.Should().BeNull();
    }

    [Fact]
    public void VerifyReject()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "sha1new=abc"));
        _store.Contains(new ManifestDigest(Sha1New: "abc")).Should().BeTrue();

        _handler.AnswerQuestionWith = true;
        _store.Verify(new ManifestDigest(Sha1New: "abc"));
        // TODO
        // _handler.LastQuestion.Should().Be(
        //     string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedAskRemove, "sha1new=abc"));

        _store.Contains(new ManifestDigest(Sha1New: "abc")).Should().BeFalse();
    }

    [Fact]
    public void AddRemoveStressTest()
    {
        StressTest.Run(() =>
        {
            try
            {
                _store.Add(_referenceDigest, builder => builder.AddFile("file", "AAA".ToStream(), 3600));
                _store.Remove(_referenceDigest);
            }
            catch (ImplementationAlreadyInStoreException)
            {}
            catch (ImplementationNotFoundException)
            {}
        });

        _store.Contains(_referenceDigest).Should().BeFalse();
    }

    [Fact]
    public void OptimiseFilesInSameImplementation()
    {
        string impl1Path = DeployImplementation("sha256=1", new TestRoot
        {
            new TestFile("fileA") {Contents = "abc"},
            new TestDirectory("dir") {new TestFile("fileB") {Contents = "abc"}}
        });

        _store.Optimise().Should().Be(3);
        _store.Optimise().Should().Be(0);
        FileUtils.AreHardlinked(
            Path.Combine(impl1Path, "fileA"),
            Path.Combine(impl1Path, "dir", "fileB")).Should().BeTrue();
    }

    [Fact]
    public void OptimiseFilesInDifferentImplementations()
    {
        string impl1Path = DeployImplementation("sha256=1", new TestRoot {new TestFile("fileA") {Contents = "abc"}});
        string impl2Path = DeployImplementation("sha256=2", new TestRoot {new TestFile("fileA") {Contents = "abc"}});

        _store.Optimise().Should().Be(3);
        _store.Optimise().Should().Be(0);
        FileUtils.AreHardlinked(
            Path.Combine(impl1Path, "fileA"),
            Path.Combine(impl2Path, "fileA")).Should().BeTrue();
    }

    [Fact]
    public void OptimiseFilesWithDifferentTimestamps()
    {
        string impl1Path = DeployImplementation("sha256=1", new TestRoot
        {
            new TestFile("fileA") {Contents = "abc", LastWrite = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)},
            new TestFile("fileX") {Contents = "abc", LastWrite = new DateTime(2000, 2, 2, 0, 0, 0, DateTimeKind.Utc)},
        });

        _store.Optimise().Should().Be(0);
        FileUtils.AreHardlinked(
            Path.Combine(impl1Path, "fileA"),
            Path.Combine(impl1Path, "fileX")).Should().BeFalse();
    }

    [Fact]
    public void OptimiseFilesWithDifferentContent()
    {
        string impl1Path = DeployImplementation("sha256=1", new TestRoot {new TestFile("fileA") {Contents = "abc"}});
        string impl2Path = DeployImplementation("sha256=2", new TestRoot {new TestFile("fileA") {Contents = "def"}});

        _store.Optimise().Should().Be(0);
        FileUtils.AreHardlinked(
            Path.Combine(impl1Path, "fileA"),
            Path.Combine(impl2Path, "fileA")).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotHardlinkAcrossManifestFormatBorders()
    {
        string impl1Path = DeployImplementation("sha256=1", new TestRoot {new TestFile("fileA") {Contents = "abc"}});
        string impl2Path = DeployImplementation("sha256new_2", new TestRoot {new TestFile("fileA") {Contents = "abc"}});

        _store.Optimise().Should().Be(0);
        FileUtils.AreHardlinked(
            Path.Combine(impl1Path, "fileA"),
            Path.Combine(impl2Path, "fileA")).Should().BeFalse();
    }

    [Fact]
    public void ListTemp()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "_How to delete"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "sha256new=test"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "other-file"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "0install-extract-abc"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "0install-remove-def"));

        _store.ListTemp().Should().BeEquivalentTo(
            Path.Combine(_tempDir, "0install-extract-abc"),
            Path.Combine(_tempDir, "0install-remove-def"));
    }

    [Fact]
    public void RemoveTemp()
    {
        string tempDir = Path.Combine(_tempDir, "0install-extract-test");
        Directory.CreateDirectory(tempDir);

        _store.RemoveTemp(tempDir).Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
    }

    [Fact]
    public void RemoveTempRejectOutsideStore()
    {
        using var tempDir = new TemporaryDirectory("0install-test");

        _store.RemoveTemp(tempDir).Should().BeFalse();
        Directory.Exists(tempDir).Should().BeTrue();
    }

    private string DeployImplementation(string id, TestRoot root)
    {
        string path = Path.Combine(_tempDir, id);
        root.Build(path);
        var builder = new ManifestBuilder(ManifestFormat.FromPrefix(id));
        new ReadDirectory(path, builder).Run();
        builder.Manifest.Save(Path.Combine(path, Manifest.ManifestFile));
        FileUtils.EnableWriteProtection(path);
        return path;
    }
}
