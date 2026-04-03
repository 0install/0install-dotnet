// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Contains test methods for <see cref="ArchiveExtractor"/>.
/// </summary>
public class ArchiveExtractorTest
{
    [Fact]
    public void TestForZip()
    {
        ArchiveExtractor.For(Archive.MimeTypeZip, new SilentTaskHandler())
                        .Should().BeOfType<ZipExtractor>();
    }

    [Fact]
    public void TestForDeb()
    {
        ArchiveExtractor.For(Archive.MimeTypeDeb, new SilentTaskHandler())
                        .Should().BeOfType<DebExtractor>();
    }

    [Fact]
    public void TestForRpm()
    {
        ArchiveExtractor.For(Archive.MimeTypeRpm, new SilentTaskHandler())
                        .Should().BeOfType<RpmExtractor>();
    }
}
