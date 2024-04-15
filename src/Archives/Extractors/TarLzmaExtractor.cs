// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using SharpCompress.Compressors.LZMA;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts LZMA-compressed TAR archives (.tar.lzma).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class TarLzmaExtractor(ITaskHandler handler) : TarExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        => base.Extract(builder, new LzmaStream(stream.Read(13), stream), subDir);
}
