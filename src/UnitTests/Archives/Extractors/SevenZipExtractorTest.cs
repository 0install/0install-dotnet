// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives.Extractors
{
    public class SevenZipExtractorTest : ArchiveExtractorTestBase
    {
        protected override string MimeType => Archive.MimeType7Z;

        [Fact]
        public void Extract()
        {
            Test(
                "testArchive.7z",
                new Manifest(ManifestFormat.Sha1New)
                {
                    [""] = {["file"] = Normal("abc")},
                    ["folder1"] = {["file"] = Normal("def")}
                });
        }

        [Fact]
        public void ExtractSubDir()
        {
            Test(
                "testArchive.7z",
                new Manifest(ManifestFormat.Sha1New)
                {
                    [""] = {["file"] = Normal("def")}
                },
                subDir: "folder1");
        }
    }
}
