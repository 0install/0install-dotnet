// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class MsiExtractorTest
    {
        [SkippableFact]
        public void TestExtract()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "MSI extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

            using var tempFile = Deploy(typeof(MsiExtractorTest).GetEmbeddedStream("testArchive.msi"));
            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            using var extractor = ArchiveExtractor.Create(tempFile, sandbox, Archive.MimeTypeMsi);
            extractor.Extract = "SourceDir";
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

        private static readonly byte[] _garbageData = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};

        [SkippableFact]
        public void TestExtractInvalidData()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "MSI extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

            using var sandbox = new TemporaryDirectory("0install-unit-tests");
            using var tempFile = Deploy(new MemoryStream(_garbageData));
            Assert.Throws<IOException>(() => ArchiveExtractor.Create(tempFile, sandbox, Archive.MimeTypeMsi));
        }

        private static TemporaryFile Deploy(Stream stream)
        {
            var tempFile = new TemporaryFile("0install-unit-tests");
            using var fileStream = File.Create(tempFile);
            stream.CopyToEx(fileStream);
            return tempFile;
        }
    }
}
#endif
