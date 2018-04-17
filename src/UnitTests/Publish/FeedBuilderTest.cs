// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Publish.EntryPoints;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Contains test methods for <see cref="FeedBuilder"/>.
    /// </summary>
    public class FeedBuilderTest : IDisposable
    {
        private readonly FeedBuilder _builder = new FeedBuilder();
        private readonly TemporaryDirectory _implementationDir = new TemporaryDirectory("0install-unit-tests");

        public void Dispose()
        {
            _builder.Dispose();
            _implementationDir.Dispose();
        }

        [Fact]
        public void TestGenerateDigest()
        {
            _builder.ImplementationDirectory = _implementationDir;
            _builder.GenerateDigest(new SilentTaskHandler());
            _builder.ID.Should().Be($"sha1new={ManifestDigest.Empty.Sha1New}");
            _builder.ManifestDigest.PartialEquals(ManifestDigest.Empty).Should().BeTrue();
        }

        [Fact]
        public void TestDetectCandidates()
        {
            _builder.ImplementationDirectory = _implementationDir;
            _builder.DetectCandidates(new SilentTaskHandler());
        }

        [Fact]
        public void TestGenerateCommands()
        {
            _builder.MainCandidate = new WindowsExe
            {
                RelativePath = "test",
                Name = "TestApp",
                Summary = "a test app",
                Version = new ImplementationVersion("1.0"),
                Architecture = new Architecture(OS.Windows, Cpu.All)
            };
            _builder.GenerateCommands();
        }

        [Fact]
        public void TestBuild()
        {
            TestGenerateDigest();
            TestDetectCandidates();
            TestGenerateCommands();

            _builder.RetrievalMethod = new Archive();
            _builder.Uri = new FeedUri("http://0install.de/feeds/test/test1.xml");
            _builder.Icons.Add(new Icon {MimeType = Icon.MimeTypePng, Href = new Uri("http://0install.de/test.png")});
            _builder.Icons.Add(new Icon {MimeType = Icon.MimeTypeIco, Href = new Uri("http://0install.de/test.ico")});
            _builder.SecretKey = new OpenPgpSecretKey(keyID: 123, fingerprint: new byte[] {1, 2, 3}, userID: "user");
            var signedFeed = _builder.Build();

            signedFeed.Feed.Name.Should().Be(_builder.MainCandidate.Name);
            signedFeed.Feed.Uri.Should().Be(_builder.Uri);
            signedFeed.Feed.Summaries.Should().Equal(new LocalizableStringCollection {_builder.MainCandidate.Summary});
            signedFeed.Feed.NeedsTerminal.Should().Be(_builder.MainCandidate.NeedsTerminal);
            signedFeed.Feed.Elements.Should().Equal(
                new Implementation
                {
                    ID = "sha1new=" + ManifestDigest.Empty.Sha1New,
                    ManifestDigest = new ManifestDigest(sha256New: ManifestDigest.Empty.Sha256New),
                    Version = _builder.MainCandidate.Version,
                    Architecture = _builder.MainCandidate.Architecture,
                    Commands = {new Command {Name = Command.NameRun, Path = "test"}},
                    RetrievalMethods = {_builder.RetrievalMethod}
                });
            signedFeed.Feed.Icons.Should().Equal(_builder.Icons);
            signedFeed.SecretKey.Should().Be(_builder.SecretKey);
        }

        [Fact]
        public void TestTemporaryDirectory()
        {
            var tempDir1 = new TemporaryDirectory("0install-unit-tests");
            var tempDir2 = new TemporaryDirectory("0install-unit-tests");

            _builder.TemporaryDirectory = tempDir1;
            Directory.Exists(tempDir1).Should().BeTrue(because: "Directory should exist");

            _builder.TemporaryDirectory = tempDir2;
            Directory.Exists(tempDir1).Should().BeFalse(because: "Directory should be auto-disposed when replaced with a new one");
            Directory.Exists(tempDir2).Should().BeTrue(because: "Directory should exist");

            _builder.Dispose();
            Directory.Exists(tempDir2).Should().BeFalse(because: "Directory should be disposed together with FeedBuilder");
        }
    }
}
