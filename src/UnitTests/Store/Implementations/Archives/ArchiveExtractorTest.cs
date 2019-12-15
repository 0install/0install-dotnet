// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Contains test methods for <see cref="ArchiveExtractor"/>.
    /// </summary>
    public class ArchiveExtractorTest
    {
        [Fact] // Ensures ArchiveExtractor.FromStream() correctly creates a ZipExtractor.
        public void TestCreateExtractor()
        {
            using var tempDir = new TemporaryDirectory("0install-unit-tests");
            string path = Path.Combine(tempDir, "a.zip");

            using (var file = File.Create(path))
            {
                using var zipStream = new ZipOutputStream(file) {IsStreamOwner = false};
                var entry = new ZipEntry("file");
                zipStream.PutNextEntry(entry);
                zipStream.CloseEntry();
            }

            using var extractor = ArchiveExtractor.Create(File.OpenRead(path), tempDir, Model.Archive.MimeTypeZip);
            extractor.Should().BeOfType<ZipExtractor>();
        }

        [Fact] // Ensures ArchiveExtractor.VerifySupport() correctly distinguishes between supported and not supported archive MIME types.
        public void TestVerifySupport()
        {
            ArchiveExtractor.VerifySupport(Model.Archive.MimeTypeZip);
            Assert.Throws<NotSupportedException>(() => ArchiveExtractor.VerifySupport("test/format"));
        }
    }
}
