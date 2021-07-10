using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    [Collection("Static state")]
    public class TarZstandardExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarZstandard;

        protected override string FileName => "testArchive.tar.zst";
    }
}
