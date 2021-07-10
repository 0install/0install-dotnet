using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations.Archives
{
    public class RubyGemExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeRubyGem;
        
        protected override string FileName => "testArchive.gem";
    }
}