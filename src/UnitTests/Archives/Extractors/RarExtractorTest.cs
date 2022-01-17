// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives.Extractors;

public class RarExtractorTest : ArchiveExtractorTestBase
{
    protected override string MimeType => Archive.MimeTypeRar;

    [Fact]
    public void Extract()
    {
        Test(
            "testArchive.rar",
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
            "testArchive.rar",
            new Manifest(ManifestFormat.Sha1New)
            {
                [""] = {["file"] = Normal("def")}
            },
            subDir: "folder1");
    }
}
