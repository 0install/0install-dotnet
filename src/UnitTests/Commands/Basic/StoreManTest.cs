// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using Moq;
using NanoByte.Common;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Implementations.Build;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// Contains integration tests for <see cref="StoreMan"/>.
    /// </summary>
    [Collection("Static state")]
    public class StoreManTest
    {
        public abstract class StoreSubCommand<T> : CliCommandTestBase<T>
            where T : StoreMan.StoreSubCommand
        {
            protected Mock<IImplementationStore> StoreMock => GetMock<IImplementationStore>();
        }

        public class Add : StoreSubCommand<StoreMan.Add>
        {
            [Fact]
            public void Archive()
            {
                using var tempFile = new TemporaryFile("0install-test-archive");
                var digest = new ManifestDigest(sha256New: "abc");
                string path = tempFile;
                StoreMock.Setup(x => x.Add(digest, Handler,
                    new ArchiveImplementationSource(path, "mime") {Extract = "extract"}))
                         .Returns("");

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_" + digest.Sha256New, path, "extract", "mime");
            }

            [Fact]
            public void ArchiveRelativePathGuessMimeType()
            {
                using var tempDir = new TemporaryWorkingDirectory("0install-test-archive");
                var digest = new ManifestDigest(sha256New: "abc");
                string path = Path.Combine(tempDir, "archive.zip");
                File.WriteAllText(path, "xyz");
                StoreMock.Setup(x => x.Add(digest, Handler,
                    new ArchiveImplementationSource(path, Model.Archive.MimeTypeZip)))
                         .Returns("");

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_" + digest.Sha256New, "archive.zip");
            }

            [Fact]
            public void MultipleArchives()
            {
                using var tempFile1 = new TemporaryFile("0install-test-archive");
                using var tempFile2 = new TemporaryFile("0install-test-archive");
                var digest = new ManifestDigest(sha256New: "abc");
                string path1 = tempFile1;
                string path2 = tempFile2;
                StoreMock.Setup(x => x.Add(digest, Handler,
                    new ArchiveImplementationSource(path1, "mime1") {Extract = "extract1"},
                    new ArchiveImplementationSource(path2, "mime2") {Extract = "extract2"}))
                         .Returns("");

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_" + digest.Sha256New,
                    path1, "extract1", "mime1",
                    path2, "extract2", "mime2");
            }

            [Fact]
            public void Directory()
            {
                using var tempDir = new TemporaryDirectory("0install-test-dir");
                var digest = new ManifestDigest(sha256New: "abc");
                string path = tempDir;
                StoreMock.Setup(x => x.Add(digest, Handler, new DirectoryImplementationSource(path, null))).Returns("");

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_" + digest.Sha256New, path);
            }

            [Fact]
            public void DirectoryRelativePath()
            {
                using var tempDir = new TemporaryWorkingDirectory("0install-test-dir");
                var digest = new ManifestDigest(sha256New: "abc");
                string path = tempDir;
                StoreMock.Setup(x => x.Add(digest, Handler, new DirectoryImplementationSource(path, null))).Returns("");

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_" + digest.Sha256New, ".");
            }
        }

        public class Audit : StoreSubCommand<StoreMan.Audit>
        {
            [Fact]
            public void TestAudit()
            {
                var storeMock = StoreMock;
                storeMock.Setup(x => x.ListAll()).Returns(new[] {new ManifestDigest("sha256new_123AB")});
                storeMock.Setup(x => x.Verify(new ManifestDigest("sha256new_123AB"), Handler));

                RunAndAssert(null, ExitCode.OK);
            }
        }

        public class Copy : StoreSubCommand<StoreMan.Copy>
        {
            [Fact]
            public void Normal()
            {
                using var tempDir = new TemporaryDirectory("0install-test-impl");
                var digest = new ManifestDigest(sha256New: "abc");
                string path = Path.Combine(tempDir, "sha256new_" + digest.Sha256New);
                StoreMock.Setup(x => x.Add(digest, Handler, new DirectoryImplementationSource(path, null))).Returns("");

                RunAndAssert(null, ExitCode.OK, path);
            }

            [Fact]
            public void RelativePath()
            {
                using var tempDir = new TemporaryWorkingDirectory("0install-test-impl");
                var digest = new ManifestDigest(sha256New: "abc");
                string path = Path.Combine(tempDir, "sha256new_" + digest.Sha256New);
                StoreMock.Setup(x => x.Add(digest, Handler, new DirectoryImplementationSource(path, null))).Returns("");

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_" + digest.Sha256New);
            }
        }

        public class Find : StoreSubCommand<StoreMan.Find>
        {
            [Fact]
            public void Test()
            {
                var digest = new ManifestDigest(sha256New: "abc");
                StoreMock.Setup(x => x.GetPath(digest)).Returns("path");

                RunAndAssert("path", ExitCode.OK,
                    "sha256new_abc");
            }
        }

        public class List : StoreSubCommand<StoreMan.List>
        {
            [Fact]
            public void Test() => RunAndAssert(new[] {StoreMock.Object}, ExitCode.OK);
        }

        public class ListImplementations : StoreSubCommand<StoreMan.ListImplementations>
        {
            [Fact]
            public void ListAll()
            {
                var testFeed = Fake.Feed;
                var testImplementation = (Implementation)testFeed.Elements[0];
                var digest1 = testImplementation.ManifestDigest;
                var digest2 = new ManifestDigest(sha256New: "2");

                using var tempDir = new TemporaryDirectory("0install-test-impl");
                GetMock<IFeedCache>().Setup(x => x.ListAll()).Returns(new[] {testFeed.Uri});
                GetMock<IFeedCache>().Setup(x => x.GetFeed(testFeed.Uri)).Returns(testFeed);
                StoreMock.Setup(x => x.ListAll()).Returns(new[] {digest1, digest2});
                StoreMock.Setup(x => x.ListAllTemp()).Returns(Array.Empty<string>());
                StoreMock.Setup(x => x.GetPath(It.IsAny<ManifestDigest>())).Returns(tempDir);
                FileUtils.Touch(Path.Combine(tempDir, ".manifest"));

                var feedNode = new FeedNode(testFeed, Sut.FeedCache);
                RunAndAssert(new ImplementationNode[] {new OwnedImplementationNode(digest1, testImplementation, feedNode, Sut.ImplementationStore), new OrphanedImplementationNode(digest2, Sut.ImplementationStore)}, ExitCode.OK);
            }
        }

        public class Optimise : StoreSubCommand<StoreMan.Optimise>
        {
            [Fact]
            public void Test()
            {
                StoreMock.Setup(x => x.Optimise(Handler)).Returns(123);

                RunAndAssert(string.Format(Resources.StorageReclaimed, 123L.FormatBytes()), ExitCode.OK);
            }
        }

        public class Purge : StoreSubCommand<StoreMan.Purge>
        {
            [Fact]
            public void Test()
            {
                var digest = new ManifestDigest(sha256New: "abc");
                StoreMock.Setup(x => x.ListAll()).Returns(new[] {digest});
                StoreMock.Setup(x => x.Remove(digest, Handler)).Returns(true);
                StoreMock.SetupGet(x => x.Path).Returns("dummy");

                Handler.AnswerQuestionWith = true;
                RunAndAssert(null, ExitCode.OK);
            }
        }

        public class Remove : StoreSubCommand<StoreMan.Remove>
        {
            [Fact]
            public void Test()
            {
                var digest = new ManifestDigest(sha256New: "abc");
                StoreMock.Setup(x => x.Remove(digest, Handler)).Returns(true);

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_abc");
            }
        }

        public class Verify : StoreSubCommand<StoreMan.Verify>
        {
            [Fact]
            public void Pass()
            {
                var digest = new ManifestDigest(sha256New: "abc");
                StoreMock.Setup(x => x.Verify(digest, Handler));

                RunAndAssert(null, ExitCode.OK,
                    "sha256new_" + digest.Sha256New);
            }

            [Fact]
            public void Fail()
            {
                var digest = new ManifestDigest(sha256New: "abc");
                StoreMock.Setup(x => x.Verify(digest, Handler)).Throws<DigestMismatchException>();

                RunAndAssert(new DigestMismatchException().Message, ExitCode.DigestMismatch,
                    "sha256new_" + digest.Sha256New);
            }
        }
    }
}
