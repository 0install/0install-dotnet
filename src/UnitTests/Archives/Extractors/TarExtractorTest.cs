// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives.Extractors
{
    public class TarExtractorTest : ArchiveExtractorTestBase
    {
        protected override string MimeType => Archive.MimeTypeTar;

        protected virtual string FileName => "testArchive.tar";

        [Fact]
        public void Extract()
        {
            Test(
                FileName,
                new Manifest(ManifestFormat.Sha1New)
                {
                    [""] =
                    {
                        ["symlink"] = Symlink("subdir1/regular"),
                        ["hardlink"] = Normal("regular\n")
                    },
                    ["subdir1"] =
                    {
                        ["regular"] = Normal("regular\n")
                    },
                    ["subdir2"] =
                    {
                        ["executable"] = Executable("executable\n")
                    }
                });
        }

        [Fact]
        public void ExtractSubDir()
        {
            Test(
                FileName,
                new Manifest(ManifestFormat.Sha1New)
                {
                    [""] =
                    {
                        ["regular"] = Normal("regular\n")
                    }
                },
                subDir: "subdir1");
        }

        [Fact]
        public void Hardlink()
        {
            using var tempDir = new TemporaryDirectory("0install-test-archive");
            var builder = new DirectoryBuilder(tempDir);
            using var stream = typeof(ArchiveExtractorTestBase).GetEmbeddedStream(FileName);
            ArchiveExtractor.For(MimeType, new SilentTaskHandler())
                            .Extract(builder, stream);

            FileUtils.AreHardlinked(Path.Combine(tempDir, "hardlink"), Path.Combine(tempDir, "subdir1", "regular"))
                     .Should().BeTrue(because: "'regular' and 'hardlink' should be hardlinked together");
        }
    }
}
