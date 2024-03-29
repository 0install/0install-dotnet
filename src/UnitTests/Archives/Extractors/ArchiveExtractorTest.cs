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
}
