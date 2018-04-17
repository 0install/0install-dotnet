// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NETCOREAPP2_0
using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class CabExtractorTest
    {
        [SkippableFact]
        public void TestExtract()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "CAB extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

            using (var sandbox = new TemporaryDirectory("0install-unit-tests"))
            using (var extractor = ArchiveExtractor.Create(typeof(CabExtractorTest).GetEmbeddedStream("testArchive.cab"), sandbox, Archive.MimeTypeCab))
            {
                extractor.Run();

                string filePath = Path.Combine(sandbox, "file");
                File.Exists(filePath).Should().BeTrue(because: "Should extract file 'file'");
                File.GetLastWriteTimeUtc(filePath).Should().Be(new DateTime(2000, 1, 1, 13, 0, 0), because: "Correct last write time should be set");
                File.ReadAllText(filePath).Should().Be("abc");

                filePath = Path.Combine(sandbox, Path.Combine("folder1", "file"));
                File.Exists(filePath).Should().BeTrue(because: "Should extract file 'dir/file'");
                File.GetLastWriteTimeUtc(filePath).Should().Be(new DateTime(2000, 1, 1, 13, 0, 0), because: "Correct last write time should be set");
                File.ReadAllText(filePath).Should().Be("def");
            }
        }

        [SkippableFact]
        public void TestExtractSubDir()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "CAB extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

            using (var sandbox = new TemporaryDirectory("0install-unit-tests"))
            using (var extractor = ArchiveExtractor.Create(typeof(CabExtractorTest).GetEmbeddedStream("testArchive.cab"), sandbox, Archive.MimeTypeCab))
            {
                extractor.Extract = "folder1";
                extractor.Run();

                string filePath = Path.Combine(sandbox, "file");
                File.Exists(filePath).Should().BeTrue(because: "Should extract file 'dir/file'");
                File.GetLastWriteTimeUtc(filePath).Should().Be(new DateTime(2000, 1, 1, 13, 0, 0), because: "Correct last write time should be set");
                File.ReadAllText(filePath).Should().Be("def");
            }
        }

        private static readonly byte[] _garbageData = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};

        [SkippableFact]
        public void TestExtractInvalidData()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "CAB extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

            using (var sandbox = new TemporaryDirectory("0install-unit-tests"))
                Assert.Throws<IOException>(() => ArchiveExtractor.Create(new MemoryStream(_garbageData), sandbox, Archive.MimeTypeCab));
        }
    }
}
#endif
