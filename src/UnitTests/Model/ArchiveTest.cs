// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Native;
using Xunit;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Contains test methods for <see cref="Archive"/>.
    /// </summary>
    public class ArchiveTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="Archive"/>.
        /// </summary>
        internal static Archive CreateTestArchive() => new()
        {
            Href = new Uri("http://example.com/test.exe"),
            MimeType = Archive.MimeTypeZip,
            Size = 128,
            StartOffset = 16,
            Extract = "extract",
            Destination = "dest"
        };

        /// <summary>
        /// Ensures that the class can be correctly cloned.
        /// </summary>
        [Fact]
        public void TestClone()
        {
            var archive1 = CreateTestArchive();
            var archive2 = archive1.Clone();

            // Ensure data stayed the same
            archive2.Should().Be(archive1, because: "Cloned objects should be equal.");
            archive2.GetHashCode().Should().Be(archive1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            archive2.Should().NotBeSameAs(archive1, because: "Cloning should not return the same reference.");
        }

        [Fact]
        public void TestNormalizeGuessMimeType()
        {
            var archive = new Archive {Href = new Uri("http://example.com/test.tar.gz"), Size = 128};
            archive.Normalize(new FeedUri("http://example.com/"));
            archive.MimeType.Should().Be(Archive.MimeTypeTarGzip, because: "Normalize() should guess missing MIME type");
        }

        [Fact]
        public void TestNormalizeLocalPath()
        {
            var archive = new Archive {Href = new Uri("test.zip", UriKind.Relative), MimeType = Archive.MimeTypeZip, Size = 128};
            archive.Normalize(new FeedUri(Path.Combine(WindowsUtils.IsWindows ? @"C:\some\dir" : "/some/dir", "feed.xml")));
            archive.Href.Should().Be(
                new Uri(WindowsUtils.IsWindows ? "file:///C:/some/dir/test.zip" : "file:///some/dir/test.zip"),
                because: "Normalize() should make relative local paths absolute");
        }
    }
}
