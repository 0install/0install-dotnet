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
            using var testDir = new TemporaryDirectory("0install-unit-tests");
            string digest = ManifestUtils.CalculateDigest(testDir, ManifestFormat.Sha256New, new SilentTaskHandler());
            digest.Should().StartWith("sha256new_");
        }

        [Fact]
        public void TestGenerateDigest()
        {
            using var testDir = new TemporaryDirectory("0install-unit-tests");
            var digest = ManifestUtils.GenerateDigest(testDir, new SilentTaskHandler());
            digest.Sha256New.Should().NotBeNull();
        }

        [Fact]
        public void TestWithRoundedTimestamps()
        {
            var unrounded = new Manifest(ManifestFormat.Sha256,
                new ManifestDirectory("dir"),
                new ManifestNormalFile("abc123", new DateTime(2000, 1, 1, 1, 0, 1), 10, "file1"),
                new ManifestExecutableFile("abc123", new DateTime(2000, 1, 1, 1, 0, 1), 10, "file2"));
            var rounded = new Manifest(ManifestFormat.Sha256,
                new ManifestDirectory("dir"),
                new ManifestNormalFile("abc123", new DateTime(2000, 1, 1, 1, 0, 2), 10, "file1"),
                new ManifestExecutableFile("abc123", new DateTime(2000, 1, 1, 1, 0, 2), 10, "file2"));
            unrounded.WithRoundedTimestamps().Should().Equal(rounded);
        }
    }
}
