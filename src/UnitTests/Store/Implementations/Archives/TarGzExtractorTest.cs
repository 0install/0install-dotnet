using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class TarGzExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarGzip;
        
        protected override string FileName => "testArchive.tar.gz";
    }
}