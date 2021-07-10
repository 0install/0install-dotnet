// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using NanoByte.Common.Native;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class MsiExtractorTest: ArchiveExtractorTestBase
    {
        protected override string MimeType => Archive.MimeTypeMsi;

        [SkippableFact]
        public void Extract()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "MSI extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

            Test(
                "testArchive.msi",
                new Manifest(ManifestFormat.Sha1New)
                {
                    [""] =
                    {
                        ["file"] = Normal("abc")
                    },
                    ["folder1"] =
                    {
                        ["file"] = Normal("def")
                    }
                },
                subDir: "SourceDir");
        }
    }
}
#endif
