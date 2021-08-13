// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives.Extractors
{
    public class ZipExtractorTest : ArchiveExtractorTestBase
    {
        protected override string MimeType => Archive.MimeTypeZip;

        [Fact]
        public void Extract()
        {
            Test(
                "testArchive.zip",
                new Manifest(ManifestFormat.Sha1New)
                {
                    [""] =
                    {
                        ["symlink"] = Symlink("subdir1/regular")
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
                "testArchive.zip",
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
        public void ExtractSubDirEmpty()
        {
            Test(
                "testArchive.zip",
                new Manifest(ManifestFormat.Sha1New),
                subDir: "subdir1/regular"); // subDir should only match directories, not files
        }
    }
}
