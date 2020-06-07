// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class SevenZipExtractorTest
    {
        [SkippableFact]
        public void TestExtract()
        {
            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            using var extractor = ArchiveExtractor.Create(typeof(SevenZipExtractorTest).GetEmbeddedStream("testArchive.7z"), sandbox, Archive.MimeType7Z);
            extractor.Run();

            string filePath = Path.Combine(sandbox, "file");
            File.Exists(filePath).Should().BeTrue(because: "Should extract file 'file'");
            File.GetLastWriteTimeUtc(filePath).Should().Be(new DateTime(2000, 1, 1, 12, 0, 0), because: "Correct last write time should be set");
            File.ReadAllText(filePath).Should().Be("abc");

            filePath = Path.Combine(sandbox, Path.Combine("folder1", "file"));
            File.Exists(filePath).Should().BeTrue(because: "Should extract file 'dir/file'");
            File.GetLastWriteTimeUtc(filePath).Should().Be(new DateTime(2000, 1, 1, 12, 0, 0), because: "Correct last write time should be set");
            File.ReadAllText(filePath).Should().Be("def");
        }

        [SkippableFact]
        public void TestExtractSubDir()
        {
            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            using var extractor = ArchiveExtractor.Create(typeof(SevenZipExtractorTest).GetEmbeddedStream("testArchive.7z"), sandbox, Archive.MimeType7Z);
            extractor.Extract = "folder1";
            extractor.Run();

            string filePath = Path.Combine(sandbox, "file");
            File.Exists(filePath).Should().BeTrue(because: "Should extract file 'dir/file'");
            File.GetLastWriteTimeUtc(filePath).Should().Be(new DateTime(2000, 1, 1, 12, 0, 0), because: "Correct last write time should be set");
            File.ReadAllText(filePath).Should().Be("def");

            Directory.GetDirectories(sandbox).Should().BeEmpty();
        }

        private static readonly byte[] _garbageData = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};

        [SkippableFact]
        public void TestExtractInvalidData()
        {
            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            Assert.Throws<EndOfStreamException>(() => ArchiveExtractor.Create(new MemoryStream(_garbageData), sandbox, Archive.MimeType7Z).Run());
        }
    }
}
