// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Compression;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts GZip-compressed TAR archives (.tar.gz).
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public partial class TarGzExtractor : TarExtractor
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        => base.Extract(builder, new GZipStream(stream, CompressionMode.Decompress), subDir);
}
