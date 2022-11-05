// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Properties;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="StoreMan"/>.
/// </summary>
public class StoreManTest
{
    private static readonly ManifestDigest _dummyDigest = new(Sha256New: "abc");

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
            using var tempFile = new TemporaryFile("0install-test");
            StoreMock.Setup(x => x.Add(_dummyDigest, It.IsAny<Action<IBuilder>>()));

            RunAndAssert(null, ExitCode.OK,
                _dummyDigest.Best!, tempFile, "extract", "mime");
        }

        [Fact]
        public void ArchiveGuessMimeType()
        {
            using var tempDir = new TemporaryDirectory("0install-test");
            string path = Path.Combine(tempDir, "archive.zip");
            FileUtils.Touch(path);
            StoreMock.Setup(x => x.Add(_dummyDigest, It.IsAny<Action<IBuilder>>()));

            RunAndAssert(null, ExitCode.OK,
                _dummyDigest.Best!, path);
        }

        [Fact]
        public void MultipleArchives()
        {
            using var tempFile1 = new TemporaryFile("0install-test");
            using var tempFile2 = new TemporaryFile("0install-test");
            StoreMock.Setup(x => x.Add(_dummyDigest, It.IsAny<Action<IBuilder>>()));

            RunAndAssert(null, ExitCode.OK,
                _dummyDigest.Best!,
                tempFile1, "extract1", "mime1",
                tempFile2, "extract2", "mime2");
        }

        [Fact]
        public void Directory()
        {
            using var tempDir = new TemporaryDirectory("0install-test");
            StoreMock.Setup(x => x.Add(_dummyDigest, It.IsAny<Action<IBuilder>>()));

            RunAndAssert(null, ExitCode.OK,
                _dummyDigest.Best!, tempDir);
        }
    }

    public class Copy : StoreSubCommand<StoreMan.Copy>
    {
        [Fact]
        public void Normal()
        {
            using var tempDir = new TemporaryDirectory("0install-test-impl");
            StoreMock.Setup(x => x.Add(_dummyDigest, It.IsAny<Action<IBuilder>>()));

            RunAndAssert(null, ExitCode.OK,
                Path.Combine(tempDir, _dummyDigest.Best!));
        }

        [Fact]
        public void RejectUnknownDigestFormat()
        {
            using var tempDir = new TemporaryDirectory("0install-test-impl");

            RunAndAssert(null, ExitCode.NotSupported,
                Path.Combine(tempDir, "unknown_digest"));
        }
    }

    public class Find : StoreSubCommand<StoreMan.Find>
    {
        [Fact]
        public void Test()
        {
            StoreMock.Setup(x => x.GetPath(_dummyDigest)).Returns("path");

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
            var digest2 = new ManifestDigest(Sha256New: "2");

            using var tempDir = new TemporaryDirectory("0install-test-impl");
            GetMock<IFeedCache>().Setup(x => x.ListAll()).Returns(new[] {testFeed.Uri});
            GetMock<IFeedCache>().Setup(x => x.GetFeed(testFeed.Uri)).Returns(testFeed);
            StoreMock.Setup(x => x.ListAll()).Returns(new[] {digest1, digest2});
            StoreMock.Setup(x => x.ListAllTemp()).Returns(Array.Empty<string>());
            StoreMock.Setup(x => x.GetPath(It.IsAny<ManifestDigest>())).Returns(tempDir);
            FileUtils.Touch(Path.Combine(tempDir, ".manifest"));

            RunAndAssert(new ImplementationNode[]
            {
                new OwnedImplementationNode(digest1, testImplementation, new FeedNode(testFeed, Sut.FeedCache), Sut.ImplementationStore),
                new OrphanedImplementationNode(digest2, Sut.ImplementationStore)
            }, ExitCode.OK);
        }
    }

    public class Optimise : StoreSubCommand<StoreMan.Optimise>
    {
        [Fact]
        public void Test()
        {
            StoreMock.Setup(x => x.Optimise()).Returns(123);

            RunAndAssert(string.Format(Resources.StorageReclaimed, 123L.FormatBytes()), ExitCode.OK);
        }
    }

    public class Purge : StoreSubCommand<StoreMan.Purge>
    {
        [Fact]
        public void Test()
        {
            StoreMock.Setup(x => x.Purge());

            Handler.AnswerQuestionWith = true;
            RunAndAssert(null, ExitCode.OK);
        }
    }

    public class Remove : StoreSubCommand<StoreMan.Remove>
    {
        [Fact]
        public void Test()
        {
            StoreMock.Setup(x => x.Remove(_dummyDigest)).Returns(true);

            RunAndAssert(null, ExitCode.OK,
                _dummyDigest.Best!);
        }
    }

    public class Verify : StoreSubCommand<StoreMan.Verify>
    {
        [Fact]
        public void Pass()
        {
            StoreMock.Setup(x => x.Verify(_dummyDigest));

            RunAndAssert(null, ExitCode.OK,
                _dummyDigest.Best!);
        }

        [Fact]
        public void Fail()
        {
            StoreMock.Setup(x => x.Verify(_dummyDigest)).Throws<DigestMismatchException>();

            RunAndAssert(new DigestMismatchException().Message, ExitCode.DigestMismatch,
                _dummyDigest.Best!);
        }
    }


    public class Audit : StoreSubCommand<StoreMan.Audit>
    {
        [Fact]
        public void TestAudit()
        {
            var digest1 = new ManifestDigest(Sha256New: "abc");
            var digest2 = new ManifestDigest(Sha256New: "xyz");

            var storeMock = StoreMock;
            storeMock.Setup(x => x.ListAll()).Returns(new[] {digest1, digest2});
            storeMock.Setup(x => x.Verify(digest1));
            storeMock.Setup(x => x.Verify(digest2));

            RunAndAssert(null, ExitCode.OK);
        }
    }
}
