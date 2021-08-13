using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Archives.Extractors
{
    [Collection("Static state")]
    public class TarZstandardExtractorTest : TarExtractorTest
    {
        protected override string MimeType => Archive.MimeTypeTarZstandard;

        protected override string FileName => "testArchive.tar.zst";
    }
}
