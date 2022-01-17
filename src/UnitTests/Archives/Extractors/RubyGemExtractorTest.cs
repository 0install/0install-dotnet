using ZeroInstall.Model;

namespace ZeroInstall.Archives.Extractors;

public class RubyGemExtractorTest : TarExtractorTest
{
    protected override string MimeType => Archive.MimeTypeRubyGem;

    protected override string FileName => "testArchive.gem";
}
