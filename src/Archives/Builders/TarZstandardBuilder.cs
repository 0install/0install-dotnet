// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZstdSharp;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a Zstandard-compressed TAR archive (tar.zst).
/// </summary>
public class TarZstandardBuilder : TarBuilder
{
    private readonly Stream _innerStream;

    /// <summary>
    /// Creates a TAR Zstandard archive builder.
    /// </summary>
    /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
    /// <param name="fast">The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</param>
    public TarZstandardBuilder(Stream stream, bool fast = false)
        : base(new CompressionStream(stream, level: fast ? 3 : 19))
    {
        // Need to keep separate reference to inner stream for disposal, because CompressionStream does not handle that
        _innerStream = stream;
    }

    public override void Dispose()
    {
        try
        {
            base.Dispose();
        }
        finally
        {
            _innerStream.Dispose();
        }
    }
}
