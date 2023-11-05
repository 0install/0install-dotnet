// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Compression;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts GZip-compressed TAR archives (.tar.gz).
/// </summary>
/// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class TarGzExtractor(ITaskHandler handler) : TarExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        => base.Extract(builder, new GZipStream(stream, CompressionMode.Decompress), subDir);
}
