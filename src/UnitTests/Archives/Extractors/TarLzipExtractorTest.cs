using ZeroInstall.Model;

namespace ZeroInstall.Archives.Extractors;

public class TarLzipExtractorTest : TarExtractorTest
{
    protected override string MimeType => Archive.MimeTypeTarLzip;

    protected override string FileName => "testArchive.tar.lz";
}
