// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;

namespace ZeroInstall.Store.Implementations.Archives
{
    public abstract class ArchiveExtractorTestBase
    {
        protected abstract string MimeType { get; }

        protected abstract string FileName { get; }

        private static readonly byte[] _garbageData = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};

        [Fact]
        public void TestExtract()
        {
            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            using var extractor = ArchiveExtractor.Create(typeof(ArchiveExtractorTestBase).GetEmbeddedStream(FileName), sandbox, MimeType);
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

        [Fact]
        public void TestExtractSubDir()
        {
            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            using var extractor = ArchiveExtractor.Create(typeof(ArchiveExtractorTestBase).GetEmbeddedStream(FileName), sandbox, MimeType);
            extractor.Extract = "folder1";
            extractor.Run();

            string filePath = Path.Combine(sandbox, "file");
            File.Exists(filePath).Should().BeTrue(because: "Should extract file 'dir/file'");
            File.GetLastWriteTimeUtc(filePath).Should().Be(new DateTime(2000, 1, 1, 12, 0, 0), because: "Correct last write time should be set");
            File.ReadAllText(filePath).Should().Be("def");

            Directory.GetDirectories(sandbox).Should().BeEmpty();
        }

        [Fact]
        public void TestExtractInvalidData()
        {
            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            ArchiveExtractor.Create(new MemoryStream(_garbageData), sandbox, MimeType).Invoking(x => x.Run()).Should().Throw<IOException>();
        }
    }
}
