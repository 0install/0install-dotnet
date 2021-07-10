// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using NanoByte.Common.Native;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class CabExtractorTest : ArchiveExtractorTestBase
    {
        protected override string MimeType => Archive.MimeTypeCab;

        private static readonly DateTime _timestamp = new(2000, 1, 1, 13, 0, 0);

        [SkippableFact]
        public void Extract()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "CAB extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

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

        [SkippableFact]
        public void ExtractSubDir()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "CAB extraction relies on a Win32 API and therefore will not work on non-Windows platforms");

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
}
#endif
