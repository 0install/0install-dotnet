// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZstdSharp;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Contains test methods for <see cref="TarZstandardBuilder"/>.
/// </summary>
public class TarZstandardBuilderTest : TarBuilderTest
{
    protected override IArchiveBuilder NewBuilder(Stream stream) => new TarZstandardBuilder(stream);

    protected override Stream GetArchiveStream() => new DecompressionStream(base.GetArchiveStream());
}
