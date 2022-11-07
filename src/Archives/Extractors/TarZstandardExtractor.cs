// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;
using ZstdSharp;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Zstandard-compressed TAR archives (tar.zst).
/// </summary>
[PrimaryConstructor]
public partial class TarZstandardExtractor : TarExtractor
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        try
        {
            using var decompressionStream = new DecompressionStream(stream);
            base.Extract(builder, decompressionStream, subDir);
        }
        #region Error handling
        catch (ZstdException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(Resources.ArchiveInvalid, ex);
        }
        #endregion
    }
}
