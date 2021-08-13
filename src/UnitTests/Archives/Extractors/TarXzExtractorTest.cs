using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Archives.Extractors
{
    [Collection("Static state")]
    public class TarXzExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarXz;

        protected override string FileName => "testArchive.tar.xz";
    }
}
