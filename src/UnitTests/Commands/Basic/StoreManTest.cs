// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using NanoByte.Common.Net;
using ZeroInstall.Archives;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="StoreMan"/>.
/// </summary>
[Collection(nameof(ImplementationServer))]
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
        public void Remote()
        {
            using var server = new MicroServer($"{_dummyDigest}.zip", new MemoryStream());
            StoreMock.Setup(x => x.Add(_dummyDigest, It.IsAny<Action<IBuilder>>()));

            RunAndAssert(null, ExitCode.OK,
                server.FileUri.ToString());
        }

        [SkippableFact]
        public void Discover()
        {
            Skip.If(!WindowsUtils.IsWindowsNT || !WindowsUtils.IsAdministrator, "Test is flaky non-Windows OSes, needs admin rights on Windows");
            ImplementationDiscovery.ExcludeLocalMachine = true;

            var digest = ManifestDigest.Empty;
            using var tempDir = new TemporaryDirectory("0install-test-store");
            using var server = new ImplementationServer(new ImplementationStore(tempDir, new SilentTaskHandler()));
            Directory.CreateDirectory(Path.Combine(tempDir, digest.Best!));

            StoreMock.Setup(x => x.Add(digest, It.IsAny<Action<IBuilder>>()));
            RunAndAssert(null, ExitCode.OK,
                $"discover:{digest}");
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
        private readonly Feed _feed1 = Fake.Feed, _feed2 = Fake.Feed;
        private readonly TemporaryFile _feedFile1 = new("0install-test-feed"), _feedFile2 = new("0install-test-feed");
        private readonly Implementation _impl1, _impl2;
        private readonly TemporaryDirectory _implDir1 = new("0install-test-impl"), _implDir2 = new("0install-test-impl"), _implDir3 = new("0install-test-impl");
        private readonly ManifestDigest _digest3 = new(Sha256New: "3");

        public ListImplementations()
        {
            _feed1.Uri = Fake.Feed1Uri;
            _impl1 = _feed1.Implementations.First();

            _feed2.Uri = Fake.Feed2Uri;
            _impl2 = _feed2.Implementations.First();
            _impl2.ManifestDigest = new(Sha256: "2");

            var feedCacheMock = GetMock<IFeedCache>();
            feedCacheMock.Setup(x => x.ListAll()).Returns([_feed1.Uri, _feed2.Uri]);
            void SetupFeed(Feed feed, string path)
            {
                feedCacheMock.Setup(x => x.GetPath(feed.Uri)).Returns(path);
                feedCacheMock.Setup(x => x.GetFeed(feed.Uri)).Returns(feed);
            }
            SetupFeed(_feed1, _feedFile1);
            SetupFeed(_feed2, _feedFile2);

            StoreMock.SetupGet(x => x.Path).Returns("dummy");
            StoreMock.Setup(x => x.ListAll()).Returns([_impl1.ManifestDigest, _impl2.ManifestDigest, _digest3]);
            void SetupImpl(ManifestDigest digest, string path)
            {
                StoreMock.Setup(x => x.GetPath(digest)).Returns(path);
                FileUtils.Touch(Path.Combine(path, ".manifest"));
            }
            SetupImpl(_impl1.ManifestDigest, _implDir1);
            SetupImpl(_impl2.ManifestDigest, _implDir2);
            SetupImpl(_digest3, _implDir3);
            StoreMock.Setup(x => x.ListTemp()).Returns([]);
        }

        public override void Dispose()
        {
            _feedFile1.Dispose();
            _feedFile2.Dispose();
            _implDir1.Dispose();
            _implDir2.Dispose();
            _implDir3.Dispose();
            base.Dispose();
        }

        [Fact]
        public void TestAll()
        {
            RunAndAssert(new[]
            {
                new OwnedImplementationNode(_implDir1, _impl1, new FeedNode(_feedFile1, _feed1)),
                new OwnedImplementationNode(_implDir2, _impl2, new FeedNode(_feedFile2, _feed2)),
                new ImplementationNode(_implDir3, _digest3)
            }, ExitCode.OK);
        }

        [Fact]
        public void TestFiltered()
        {
            RunAndAssert(new[]
            {
                new OwnedImplementationNode(_implDir2, _impl2, new FeedNode(_feedFile2, _feed2))
            }, ExitCode.OK, _feed2.Uri!.ToStringRfc());
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
            storeMock.Setup(x => x.ListAll()).Returns([digest1, digest2]);
            storeMock.Setup(x => x.Verify(digest1));
            storeMock.Setup(x => x.Verify(digest2));

            RunAndAssert(null, ExitCode.OK);
        }
    }
}
