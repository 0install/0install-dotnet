// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class ZipExtractorTest : IDisposable
    {
        private static TestRoot SamplePackageHierarchy => new()
        {
            new TestFile("file") {Contents = "First file"},
            new TestFile("file2") {Contents = ""},
            new TestDirectory("emptyFolder"),
            new TestDirectory("sub")
            {
                new TestDirectory("folder")
                {
                    new TestFile("nestedFile") {Contents = "File 3\n"},
                    new TestDirectory("nestedFolder")
                    {
                        new TestFile("doublyNestedFile") {Contents = "File 4"}
                    }
                }
            }
        };

        private readonly byte[] _archiveData = BuildArchive();
        private readonly TemporaryDirectory _sandbox = new("0install-test-archives");
        public void Dispose() => _sandbox.Dispose();

        private static byte[] BuildArchive()
        {
            using var tempDir = new TemporaryDirectory("0install-test-archives");
            using var archiveStream = new MemoryStream();
            SamplePackageHierarchy.Build(tempDir);
            using (var generator = new ZipGenerator(tempDir, archiveStream))
                generator.Run();
            return archiveStream.ToArray();
        }

        [Fact]
        public void ComplexHierarchy()
        {
            using (var extractor = ArchiveExtractor.Create(typeof(ZipExtractorTest).GetEmbeddedStream("testArchive.zip"), _sandbox, Archive.MimeTypeZip))
                extractor.Run();

            new TestRoot
            {
                new TestSymlink("symlink", "subdir1/regular"),
                new TestDirectory("subdir1")
                {
                    new TestFile("regular") {LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                },
                new TestDirectory("subdir2")
                {
                    new TestFile("executable") {IsExecutable = true, LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                }
            }.Verify(_sandbox);
        }

        [Fact]
        public void Suffix()
        {
            using (var extractor = ArchiveExtractor.Create(new MemoryStream(_archiveData), _sandbox, Archive.MimeTypeZip))
            {
                extractor.TargetSuffix = "suffix";
                extractor.Run();
            }

            SamplePackageHierarchy.Verify(Path.Combine(_sandbox, "suffix"));
        }

        [Fact]
        public void ExtractionOfSubDir()
        {
            using (var extractor = ArchiveExtractor.Create(new MemoryStream(_archiveData), _sandbox, Archive.MimeTypeZip))
            {
                extractor.Extract = "/sub/folder/";
                extractor.Run();
            }

            new TestRoot
            {
                new TestFile("nestedFile") {Contents = "File 3\n"},
                new TestDirectory("nestedFolder")
                {
                    new TestFile("doublyNestedFile") {Contents = "File 4"}
                }
            }.Verify(_sandbox);
        }

        [Fact]
        public void EnsureSubDirDoesNotTouchFileNames()
        {
            using (var extractor = ArchiveExtractor.Create(new MemoryStream(_archiveData), _sandbox, Archive.MimeTypeZip))
            {
                extractor.Extract = "/sub/folder/nested";
                extractor.Run();
            }

            Directory.Exists(Path.Combine(_sandbox, "Folder")).Should().BeFalse(because: "Should not apply subdir matching to part of filename");
            File.Exists(Path.Combine(_sandbox, "File")).Should().BeFalse(because: "Should not apply subdir matching to part of filename");
        }

        [Fact]
        public void ExtractOverwritingExistingItems()
        {
            new TestRoot
            {
                new TestFile("file0") {Contents = "This file should not be touched"},
                new TestFile("file1") {Contents = "Wrong content"}
            }.Build(_sandbox);

            using (var extractor = ArchiveExtractor.Create(new MemoryStream(_archiveData), _sandbox, Archive.MimeTypeZip))
                extractor.Run();

            new TestRoot
            {
                new TestFile("file0") {Contents = "This file should not be touched"}
            }.Verify(_sandbox);
            SamplePackageHierarchy.Verify(_sandbox);
        }

        /// <summary>
        /// Tests whether the extractor correctly handles a ZIP archive containing an executable file.
        /// </summary>
        [Fact]
        public void ExtractUnixArchiveWithExecutable()
        {
            using (var extractor = new ZipExtractor(typeof(ZipExtractorTest).GetEmbeddedStream("testArchive.zip"), _sandbox))
                extractor.Run();

            new TestRoot
            {
                new TestDirectory("subdir2")
                {
                    new TestFile("executable") {IsExecutable = true, LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                }
            }.Verify(_sandbox);
        }

        /// <summary>
        /// TTests whether the extractor correctly handles a ZIP archive containing containing a symbolic link.
        /// </summary>
        [Fact]
        public void ExtractUnixArchiveWithSymlink()
        {
            using (var extractor = new ZipExtractor(typeof(ZipExtractorTest).GetEmbeddedStream("testArchive.zip"), _sandbox))
                extractor.Run();

            new TestRoot
            {
                new TestSymlink("symlink", target: "subdir1/regular"),
                new TestDirectory("subdir1")
                {
                    new TestFile("regular") {LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                }
            }.Verify(_sandbox);
        }
    }
}
