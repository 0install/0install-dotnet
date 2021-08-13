using ZeroInstall.Model;

namespace ZeroInstall.Archives.Extractors
{
    public class TarLzmaExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarLzma;

        protected override string FileName => "testArchive.tar.lzma";
    }
}
