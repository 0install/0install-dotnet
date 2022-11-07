// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZstdSharp;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a Zstandard-compressed TAR archive (tar.zst).
/// </summary>
public class TarZstandardBuilder : TarBuilder
{
    /// <summary>
    /// Creates a TAR Zstandard archive builder.
    /// </summary>
    /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
    public TarZstandardBuilder(Stream stream)
        : base(new CompressionStream(stream))
    {}
}
