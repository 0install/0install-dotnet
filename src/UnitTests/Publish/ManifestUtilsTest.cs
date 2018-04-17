// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Contains test methods for <see cref="ManifestUtils"/>.
    /// </summary>
    public class ManifestUtilsTest
    {
        [Fact]
        public void TestCalculateDigest()
        {
            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                string digest = ManifestUtils.CalculateDigest(testDir, ManifestFormat.Sha256New, new SilentTaskHandler());
                digest.Should().StartWith("sha256new_");
            }
        }

        [Fact]
        public void TestGenerateDigest()
        {
            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                var digest = ManifestUtils.GenerateDigest(testDir, new SilentTaskHandler());
                digest.Sha256New.Should().NotBeNull();
            }
        }
    }
}
