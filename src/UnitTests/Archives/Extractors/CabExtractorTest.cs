// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives.Extractors;

public class CabExtractorTest : ArchiveExtractorTestBase
{
    protected override string MimeType => Archive.MimeTypeCab;

    private static readonly DateTime _timestamp = new(2000, 1, 1, 13, 0, 0);

    public CabExtractorTest()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "CAB extraction relies on a Win32 API and therefore will not work on non-Windows platforms");
    }

    [Fact]
    public void Extract()
    {
        Test(
            "testArchive.cab",
            new Manifest(ManifestFormat.Sha1New)
            {
                [""] =
                {
                    ["file"] = Normal("abc", _timestamp)
                },
                ["folder1"] =
                {
                    ["file"] = Normal("def", _timestamp)
                }
            });
    }

    [Fact]
    public void ExtractSubDir()
    {
        Test(
            "testArchive.cab",
            new Manifest(ManifestFormat.Sha1New)
            {
                [""] =
                {
                    ["file"] = Normal("def", _timestamp)
                }
            },
            subDir: "folder1");
    }
}
