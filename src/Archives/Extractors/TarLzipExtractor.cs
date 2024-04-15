// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using SharpCompress.Compressors;
using SharpCompress.Compressors.LZMA;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Lzip-compressed TAR archives (.tar.lz).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class TarLzipExtractor(ITaskHandler handler) : TarExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        => base.Extract(builder, new LZipStream(stream, CompressionMode.Decompress), subDir);
}
