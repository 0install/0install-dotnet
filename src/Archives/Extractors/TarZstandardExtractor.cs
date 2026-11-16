// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using ZeroInstall.Store.FileSystem;

#if NET11_0_OR_GREATER
using System.IO.Compression;
#else
using ZstdSharp;
#endif

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Zstandard-compressed TAR archives (tar.zst).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class TarZstandardExtractor(ITaskHandler handler) : TarExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        try
        {
#if NET11_0_OR_GREATER
            using var decompressionStream = new ZstandardStream(stream, CompressionMode.Decompress, leaveOpen: true);
#else
            using var decompressionStream = new DecompressionStream(stream);
#endif
            base.Extract(builder, decompressionStream, subDir);
        }
        #region Error handling
#if NET11_0_OR_GREATER
        catch (ArgumentException ex)
#else
        catch (Exception ex) when (ex is ZstdException or ArgumentException)
#endif
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(Resources.ArchiveInvalid, ex);
        }
        #endregion
    }
}
#endif
