using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    [Collection("Static state")]
    public class TarXzExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarXz;

        protected override string FileName => "testArchive.tar.xz";
    }
}
