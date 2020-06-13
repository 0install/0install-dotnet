// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class RarExtractorTest : ArchiveExtractorTestBase
    {
        protected override string MimeType => Archive.MimeTypeRar;

        protected override string FileName => "testArchive.rar";
    }
}
