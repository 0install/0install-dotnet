// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.FileSystem;

public class DirectoryListingCacheTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir = new("0install-unit-test-listing");
    private readonly DirectoryListingCache _cache = new();

    public void Dispose() => _tempDir.Dispose();

    private string Full(params string[] parts) => Path.Combine([_tempDir, ..parts]);

    [Fact]
    public void ReturnsExactName()
    {
        File.WriteAllText(Full("file"), "data");

        var entry = _cache.TryGetDirectoryEntry(Full("file"));
        entry.Should().NotBeNull();
        entry!.Value.Name.Should().Be("file");
        entry.Value.Attributes.Should().NotHaveFlag(FileAttributes.Directory);
    }

    [Fact]
    public void ReturnsDirectoryAttributes()
    {
        Directory.CreateDirectory(Full("dir"));

        _cache.TryGetDirectoryEntry(Full("dir"))!.Value.Attributes.Should().HaveFlag(FileAttributes.Directory);
    }

    [Fact]
    public void ReturnsNullForMissing()
    {
        _cache.TryGetDirectoryEntry(Full("missing")).Should().BeNull();
        _cache.TryGetDirectoryEntry(Full("missing", "nested")).Should().BeNull();
    }

    [Theory]
    [InlineData("File", "file")]
    [InlineData("é", "é")]
    [InlineData("é", "é")]
    public void ReturnsStoredNameForAlias(string stored, string requested)
    {
        File.WriteAllText(Full(stored), "data");

        _cache.TryGetDirectoryEntry(Full(requested))!.Value.Name.Should().Be(stored);
    }

    [Fact]
    public void PicksUpUnreportedEntries()
    {
        _cache.TryGetDirectoryEntry(Full("file")).Should().BeNull();
        File.WriteAllText(Full("file"), "data");

        _cache.TryGetDirectoryEntry(Full("file"))!.Value.Name.Should().Be("file");
    }

    [Fact]
    public void TracksAdded()
    {
        _cache.TryGetDirectoryEntry(Full("other")).Should().BeNull(); // Populate cache
        Directory.CreateDirectory(Full("dir", "sub"));
        _cache.Added(Full("dir", "sub"), _tempDir);

        _cache.TryGetDirectoryEntry(Full("DIR"))!.Value.Name.Should().Be("dir");
        _cache.TryGetDirectoryEntry(Full("dir", "SUB"))!.Value.Name.Should().Be("sub");
    }

    [Fact]
    public void TracksRename()
    {
        File.WriteAllText(Full("a"), "data");
        _cache.TryGetDirectoryEntry(Full("a")).Should().NotBeNull(); // Populate cache

        File.Move(Full("a"), Full("b"));
        _cache.Removed(Full("a"));
        _cache.Added(Full("b"), _tempDir);

        _cache.TryGetDirectoryEntry(Full("a")).Should().BeNull();
        _cache.TryGetDirectoryEntry(Full("A")).Should().BeNull();
        _cache.TryGetDirectoryEntry(Full("B"))!.Value.Name.Should().Be("b");
    }

    [Fact]
    public void TracksRemovedDirectory()
    {
        Directory.CreateDirectory(Full("dir"));
        File.WriteAllText(Full("dir", "file"), "data");
        _cache.TryGetDirectoryEntry(Full("dir", "file")).Should().NotBeNull(); // Populate cache

        Directory.Delete(Full("dir"), recursive: true);
        _cache.Removed(Full("dir"));
        Directory.CreateDirectory(Full("dir"));
        File.WriteAllText(Full("dir", "File"), "data");
        _cache.Added(Full("dir", "File"), _tempDir);

        _cache.TryGetDirectoryEntry(Full("dir"))!.Value.Name.Should().Be("dir");
        _cache.TryGetDirectoryEntry(Full("dir", "file"))!.Value.Name.Should().Be("File", because: "the listing of the removed directory must not be reused");
    }
}
