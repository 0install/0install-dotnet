// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class SevenZipExtractorTest : ArchiveExtractorTestBase
    {
        protected override string MimeType => Archive.MimeType7Z;

        protected override string FileName => "testArchive.7z";
    }
}
