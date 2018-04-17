// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Services.Fetchers
{
    /// <summary>
    /// Ensures that <see cref="RetrievalMethodRanker"/> correctly ranks <see cref="RetrievalMethod"/>s.
    /// </summary>
    public class RetrievalMethodRankerTest
    {
        [Fact]
        public void ArchiveOverRecipe() => AssertOver(new Archive(), new Recipe());

        [Fact]
        public void SingleFileOverRecipe() => AssertOver(new SingleFile(), new Recipe());

        [Fact]
        public void SmallOverLarge() => AssertOver(new Archive {Size = 10}, new SingleFile {Size = 20});

        [Fact]
        public void ArchiveAndSingleFileEqual()
        {
            RetrievalMethodRanker.Instance.Compare(new Archive(), new SingleFile()).Should().Be(0);
            RetrievalMethodRanker.Instance.Compare(new SingleFile(), new Archive()).Should().Be(0);
        }

        [Fact]
        public void ShortRecipeOverLong() => AssertOver(new Recipe {Steps = {new Archive()}}, new Recipe {Steps = {new Archive(), new Archive()}});

        /// <summary>
        /// Asserts that <paramref name="x"/> is ranked over <paramref name="y"/>.
        /// </summary>
        private static void AssertOver(RetrievalMethod x, RetrievalMethod y)
        {
            RetrievalMethodRanker.Instance.Compare(x, y).Should().BeLessThan(0);
            RetrievalMethodRanker.Instance.Compare(y, x).Should().BeGreaterThan(0);
        }
    }
}
