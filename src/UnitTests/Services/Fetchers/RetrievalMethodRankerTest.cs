// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Fetchers;

/// <summary>
/// Ensures that <see cref="RetrievalMethodRanker"/> correctly ranks <see cref="RetrievalMethod"/>s.
/// </summary>
public class RetrievalMethodRankerTest
{
    private readonly Archive _archive = new() {Href = new("http://example.com/archive.zip")};
    private readonly SingleFile _singleFile = new() {Href = new("http://example.com/file.jar"), Destination = "file.jar"};

    [Fact]
    public void ArchiveOverRecipe() => AssertOver(_archive, new Recipe());

    [Fact]
    public void SingleFileOverRecipe() => AssertOver(_singleFile, new Recipe());

    [Fact]
    public void SmallOverLarge() => AssertOver(
        new Archive {Href = new("http://example.com/archive.zip"), Size = 10},
        new SingleFile {Href = new("http://example.com/file.jar"), Destination = "file.jar", Size = 20});

    [Fact]
    public void ArchiveAndSingleFileEqual()
    {
        RetrievalMethodRanker.Instance.Compare(_archive, _singleFile).Should().Be(0);
        RetrievalMethodRanker.Instance.Compare(_singleFile, _archive).Should().Be(0);
    }

    [Fact]
    public void ShortRecipeOverLong() => AssertOver(new Recipe {Steps = {_archive}}, new Recipe {Steps = {_archive, _archive}});

    /// <summary>
    /// Asserts that <paramref name="x"/> is ranked over <paramref name="y"/>.
    /// </summary>
    private static void AssertOver(RetrievalMethod x, RetrievalMethod y)
    {
        RetrievalMethodRanker.Instance.Compare(x, y).Should().BeLessThan(0);
        RetrievalMethodRanker.Instance.Compare(y, x).Should().BeGreaterThan(0);
    }
}
