// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts GZip-compressed TAR archives (.tar.gz).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [PrimaryConstructor]
    public partial class TarGzExtractor : TarExtractor
    {
        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        {
            try
            {
                base.Extract(builder, new GZipInputStream(stream) {IsStreamOwner = false}, subDir);
            }
            #region Error handling
            catch (GZipException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }
    }
}
