// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Manifests;
using static System.Runtime.InteropServices.RuntimeInformation;
using static System.Runtime.InteropServices.OSPlatform;

namespace ZeroInstall.Archives.Extractors;

public class RpmExtractorTest : ArchiveExtractorTestBase
{
    public RpmExtractorTest()
    {
        Skip.IfNot(IsOSPlatform(Linux), "RPM extraction relies on rpm2cpio and cpio and therefore will not work on non-Linux platforms");
    }

    protected override string MimeType => Archive.MimeTypeRpm;

    [SkippableFact]
    public void Extract()
    {
        Test(
            "testArchive.rpm",
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
            "testArchive.rpm",
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
            "testArchive.rpm",
            new Manifest(ManifestFormat.Sha1New),
            subDir: "subdir1/regular"); // subDir should only match directories, not files
    }
}
