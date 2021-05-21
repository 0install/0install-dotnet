// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
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
            using var testDir = new TemporaryDirectory("0install-test-manifest");
            string digest = ManifestUtils.CalculateDigest(testDir, ManifestFormat.Sha256New, new SilentTaskHandler());
            digest.Should().StartWith("sha256new_");
        }

        [Fact]
        public void TestGenerateDigest()
        {
            using var testDir = new TemporaryDirectory("0install-test-manifest");
            var digest = ManifestUtils.GenerateDigest(testDir, new SilentTaskHandler());
            digest.Sha256New.Should().NotBeNull();
        }

        [Fact]
        public void TestWithOffset()
        {
            var original = new Manifest(ManifestFormat.Sha256,
                new ManifestDirectory("dir"),
                new ManifestNormalFile("abc123", new DateTime(2000, 1, 1, 1, 0, 1, DateTimeKind.Utc), 10, "file1"),
                new ManifestExecutableFile("abc123", new DateTime(2000, 1, 1, 1, 0, 1, DateTimeKind.Utc), 10, "file2"));
            var offset = new Manifest(ManifestFormat.Sha256,
                new ManifestDirectory("dir"),
                new ManifestNormalFile("abc123", new DateTime(2000, 1, 1, 2, 0, 2, DateTimeKind.Utc), 10, "file1"),
                new ManifestExecutableFile("abc123", new DateTime(2000, 1, 1, 2, 0, 2, DateTimeKind.Utc), 10, "file2"));
            original.WithOffset(TimeSpan.FromHours(1)).Should().Equal(offset);
        }
    }
}
