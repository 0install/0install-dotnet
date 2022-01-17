namespace ZeroInstall.Archives.Extractors;

public class TarZstandardExtractorTest : TarExtractorTest
{
    protected override string MimeType => Archive.MimeTypeTarZstandard;

    protected override string FileName => "testArchive.tar.zst";
}
