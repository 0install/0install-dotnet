// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common.Streams;
using SharpCompress.Compressors.LZMA;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts LZMA-compressed TAR archives (.tar.lzma).
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public partial class TarLzmaExtractor : TarExtractor
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        => base.Extract(builder, new LzmaStream(stream.Read(13), stream), subDir);
}
