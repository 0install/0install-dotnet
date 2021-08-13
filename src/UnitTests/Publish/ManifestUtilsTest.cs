// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Contains test methods for <see cref="ManifestUtils"/>.
    /// </summary>
    public class ManifestUtilsTest
    {
        [Fact]
        public void CalculateDigest()
        {
            using var testDir = new TemporaryDirectory("0install-test-manifest");
            string digest = ManifestUtils.CalculateDigest(testDir, ManifestFormat.Sha256New, new SilentTaskHandler());
            digest.Should().StartWith("sha256new_");
        }

        [Fact]
        public void GenerateDigest()
        {
            using var testDir = new TemporaryDirectory("0install-test-manifest");
            var digest = ManifestUtils.GenerateDigest(testDir, new SilentTaskHandler());
            digest.Sha256New.Should().NotBeNull();
        }
    }
}
