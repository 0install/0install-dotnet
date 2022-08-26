// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives.Extractors;

public class DmgExtractorTest : ArchiveExtractorTestBase
{
    public DmgExtractorTest()
    {
        Skip.IfNot(UnixUtils.IsMacOSX, "DMG extraction relies on hdutil and therefore will not work on non-MacOS platforms");
    }

    protected override string MimeType => Archive.MimeTypeDmg;

    [SkippableFact]
    public void Extract()
    {
        Test(
            "testArchive.dmg",
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

    [SkippableFact]
    public void ExtractSubDir()
    {
        Test(
            "testArchive.dmg",
            new Manifest(ManifestFormat.Sha1New)
            {
                [""] =
                {
                    ["regular"] = Normal("regular\n")
                }
            },
            subDir: "subdir1");
    }

    [SkippableFact]
    public void ExtractSubDirEmpty()
    {
        Test(
            "testArchive.dmg",
            new Manifest(ManifestFormat.Sha1New),
            subDir: "subdir1/regular"); // subDir should only match directories, not files
    }
}
