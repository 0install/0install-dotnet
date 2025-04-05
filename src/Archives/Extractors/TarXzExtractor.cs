// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using NanoByte.Common.Streams;
using SharpCompress.Compressors.Xz;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts XZ-compressed TAR archives (tar.xz).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class TarXzExtractor(ITaskHandler handler) : TarExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        => base.Extract(builder, new XZStream(
            stream.WithSeekBuffer(bufferSize: 0) // Allow skipping past padding
        ), subDir);
}
#endif
