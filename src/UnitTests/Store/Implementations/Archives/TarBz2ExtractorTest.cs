using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class TarBz2ExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarBzip;
        
        protected override string FileName => "testArchive.tar.bz2";
    }
}