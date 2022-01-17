namespace ZeroInstall.Archives.Extractors;

public class TarBz2ExtractorTest : TarExtractorTest
{
    protected override string MimeType => Archive.MimeTypeTarBzip;

    protected override string FileName => "testArchive.tar.bz2";
}
