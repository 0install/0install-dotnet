// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ImpromptuNinjas.ZStd;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
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
                base.Extract(builder, new ZStdDecompressStream(stream), subDir);
            }
            #region Error handling
            catch (ZStdException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }
    }
}
