// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Model;
using ZeroInstall.Services.Native;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.Services.Fetchers
{
    /// <summary>
    /// Runs test methods for <see cref="Fetcher"/>.
    /// </summary>
    public class FetcherTest : TestWithMocks
    {
        private static readonly Stream _zipArchiveStream = typeof(FetcherTest).GetEmbeddedStream("testArchive.zip");
        private static readonly DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly MockTaskHandler _handler = new();
        private readonly Config _config = new();
        private readonly Mock<IImplementationStore> _storeMock;
        private readonly Fetcher _fetcher;

        public FetcherTest()
        {
            _storeMock = CreateMock<IImplementationStore>();
            _fetcher = new Fetcher(_config, _storeMock.Object, _handler);
        }

        [Fact]
        public void DownloadSingleArchive()
        {
            using var server = new MicroServer("archive.zip", _zipArchiveStream);
            TestDownloadArchives(new Archive
            {
                Href = server.FileUri,
                MimeType = Archive.MimeTypeZip,
                Size = _zipArchiveStream.Length,
                Extract = "extract",
                Destination = "destination"
            });
        }

        [Fact]
        public void DownloadLocalArchive()
        {
            using var tempFile = new TemporaryFile("0install-test-archive");
            _zipArchiveStream.CopyToFile(tempFile);
            TestDownloadArchives(new Archive
            {
                Href = new(tempFile),
                MimeType = Archive.MimeTypeZip,
                Size = _zipArchiveStream.Length,
                Extract = "extract",
                Destination = "destination"
            });
        }

        [Fact]
        public void DownloadMultipleArchives()
        {
            using var server1 = new MicroServer("archive.zip", _zipArchiveStream);
            using var server2 = new MicroServer("archive.zip", _zipArchiveStream);
            TestDownloadArchives(
                new Archive {Href = server1.FileUri, MimeType = Archive.MimeTypeZip, Size = _zipArchiveStream.Length, Extract = "extract1", Destination = "destination1"},
                new Archive {Href = server2.FileUri, MimeType = Archive.MimeTypeZip, Size = _zipArchiveStream.Length, Extract = "extract2", Destination = "destination2"});
        }

        [Fact]
        public void DownloadSingleFile()
        {
            using var server = new MicroServer("regular", TestFile.DefaultContents.ToStream());
            TestDownload(
                new TestRoot {new TestFile("regular") {LastWrite = _unixEpoch}},
                new SingleFile {Href = server.FileUri, Size = TestFile.DefaultContents.Length, Destination = "regular"});
        }

        [Fact]
        public void DownloadRecipe()
        {
            using var serverArchive = new MicroServer("archive.zip", _zipArchiveStream);
            using var serverSingleFile = new MicroServer("regular", TestFile.DefaultContents.ToStream());
            TestDownload(
                new TestRoot
                {
                    new TestFile("regular") {LastWrite = new DateTime(2000, 1, 1, 11, 0, 0, DateTimeKind.Utc)},
                    new TestFile("regular2") {LastWrite = _unixEpoch},
                    new TestFile("executable2") {IsExecutable = true, LastWrite = new DateTime(2000, 1, 1, 11, 0, 0, DateTimeKind.Utc)}
                },
                new Recipe
                {
                    Steps =
                    {
                        new Archive {Href = serverArchive.FileUri, MimeType = Archive.MimeTypeZip, Size = _zipArchiveStream.Length},
                        new RenameStep {Source = "executable", Destination = "executable2"},
                        new SingleFile {Href = serverSingleFile.FileUri, Size = TestFile.DefaultContents.Length, Destination = "regular2"}
                    }
                });
        }

        [Fact]
        public void SkipBroken()
        {
            using var serverArchive = new MicroServer("archive.zip", _zipArchiveStream);
            using var serverSingleFile = new MicroServer("regular", TestFile.DefaultContents.ToStream());
            TestDownload(
                new TestRoot {new TestFile("regular") {LastWrite = _unixEpoch}},
                // broken: wrong size
                new Archive {Href = serverArchive.FileUri, MimeType = Archive.MimeTypeZip, Size = 0},
                // broken: unknown archive format
                new Archive {Href = serverArchive.FileUri, MimeType = "test/format", Size = _zipArchiveStream.Length},
                // works
                new Recipe {Steps = {new SingleFile {Href = serverSingleFile.FileUri, Size = TestFile.DefaultContents.Length, Destination = "regular"}}});
        }

        #region Helpers
        private void TestDownloadArchives(params Archive[] archives)
        {
            var digest = new ManifestDigest(sha256New: "test123");
            var archiveSources = archives.Select(archive => new ArchiveImplementationSource("dummy", archive.MimeType!)
            {
                Extract = archive.Extract,
                Destination = archive.Destination,
                StartOffset = archive.StartOffset,
                OriginalSource = archive.Href!.ToStringRfc()
            });
            var testImplementation = new Implementation {ID = "test", ManifestDigest = digest, RetrievalMethods = {GetRetrievalMethod(archives)}};

            _storeMock.Setup(x => x.GetPath(digest)).Returns(() => null);

            _storeMock.Setup(x => x.Add(digest, _handler, It.Is<IImplementationSource[]>(sources => sources.IsEquivalentTo(archiveSources))));

            _fetcher.Fetch(testImplementation);
        }

        private static RetrievalMethod GetRetrievalMethod(Archive[] archives)
        {
            if (archives.Length == 1) return archives[0];
            else
            {
                var recipe = new Recipe();
                recipe.Steps.AddRange(archives);
                return recipe;
            }
        }

        private void TestDownload(TestRoot expected, params RetrievalMethod[] retrievalMethod)
        {
            var digest = new ManifestDigest(sha256New: "test123");
            var testImplementation = new Implementation {ID = "test", ManifestDigest = digest};
            testImplementation.RetrievalMethods.AddRange(retrievalMethod);

            _storeMock.Setup(x => x.GetPath(digest))
                      .Returns(() => null);

            _storeMock.Setup(x => x.Add(digest, _handler, It.IsAny<IImplementationSource[]>()))
                      .Callback(VerifyDirectorySource(expected));

            _fetcher.Fetch(testImplementation);
        }

        private static Action<ManifestDigest, ITaskHandler, IImplementationSource[]> VerifyDirectorySource(TestRoot expected)
            => (_, _, steps) => expected.Verify(((DirectoryImplementationSource)steps[0]).Path);
        #endregion

        [Fact]
        public void RunExternalConfirm()
        {
            bool installInvoked = false;
            _handler.AnswerQuestionWith = true;
            _fetcher.Fetch(new Implementation
            {
                ID = ExternalImplementation.PackagePrefix + "123",
                RetrievalMethods =
                {
                    new ExternalRetrievalMethod
                    {
                        ConfirmationQuestion = "Install?",
                        Install = () => { installInvoked = true; }
                    }
                }
            });
            installInvoked.Should().BeTrue();
        }

        [Fact]
        public void RunExternalDeny()
        {
            bool installInvoked = false;
            _handler.AnswerQuestionWith = false;
            Assert.Throws<OperationCanceledException>(() => _fetcher.Fetch(new Implementation
            {
                ID = ExternalImplementation.PackagePrefix + "123",
                RetrievalMethods =
                {
                    new ExternalRetrievalMethod
                    {
                        ConfirmationQuestion = "Install?",
                        Install = () => { installInvoked = true; }
                    }
                }
            }));
            installInvoked.Should().BeFalse();
        }

        [Fact]
        public void SkipExisting()
        {
            var digest = new ManifestDigest(sha256New: "test123");
            var testImplementation = new Implementation {ID = "test", ManifestDigest = digest, RetrievalMethods = {new Recipe()}};
            _storeMock.Setup(x => x.GetPath(digest)).Returns("dummy");

            _fetcher.Fetch(testImplementation);
        }

        [Fact]
        public void NoSuitableMethod()
        {
            var implementation = new Implementation {ID = "test", ManifestDigest = new ManifestDigest(sha256New: "test123")};
            _storeMock.Setup(x => x.GetPath(implementation.ManifestDigest)).Returns(() => null);

            Assert.Throws<NotSupportedException>(() => _fetcher.Fetch(implementation));
        }

        [Fact]
        public void UnsupportedArchiveFormat()
        {
            var implementation = new Implementation
            {
                ID = "test",
                ManifestDigest = new ManifestDigest(sha256New: "test123"),
                RetrievalMethods = {new Archive {MimeType = "test/format"}}
            };
            _storeMock.Setup(x => x.GetPath(implementation.ManifestDigest)).Returns(() => null);

            Assert.Throws<NotSupportedException>(() => _fetcher.Fetch(implementation));
        }

        [Fact]
        public void UnsupportedArchiveFormatInRecipe()
        {
            var implementation = new Implementation
            {
                ID = "test",
                ManifestDigest = new ManifestDigest(sha256New: "test123"),
                RetrievalMethods = {new Recipe {Steps = {new Archive {MimeType = Archive.MimeTypeZip}, new Archive {MimeType = "test/format"}}}}
            };
            _storeMock.Setup(x => x.GetPath(implementation.ManifestDigest)).Returns(() => null);

            Assert.Throws<NotSupportedException>(() => _fetcher.Fetch(implementation));
        }

        [Fact]
        public void DownloadSingleArchiveMirror()
        {
            using var mirrorServer = new MicroServer("archive/http/invalid/directory%23archive.zip", _zipArchiveStream);
            _config.FeedMirror = new FeedUri(mirrorServer.ServerUri);
            TestDownloadArchives(new Archive
            {
                Href = new("http://invalid/directory/archive.zip"),
                MimeType = Archive.MimeTypeZip,
                Size = _zipArchiveStream.Length,
                Extract = "extract",
                Destination = "destination"
            });
        }
    }
}
