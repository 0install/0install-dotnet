// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NET11_0_OR_GREATER
using System.IO.Compression;
#else
using ZstdSharp;
#endif

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Contains test methods for <see cref="TarZstandardBuilder"/>.
/// </summary>
public class TarZstandardBuilderTest : TarBuilderTest
{
    protected override IArchiveBuilder NewBuilder(Stream stream) => new TarZstandardBuilder(stream);

#if NET11_0_OR_GREATER
    protected override Stream GetArchiveStream() => new ZstandardStream(base.GetArchiveStream(), CompressionMode.Decompress);
#else
    protected override Stream GetArchiveStream() => new DecompressionStream(base.GetArchiveStream());
#endif
}
