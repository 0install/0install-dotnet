using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class TarLzipExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarLzip;
        
        protected override string FileName => "testArchive.tar.lz";
    }
}