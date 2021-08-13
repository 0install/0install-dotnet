// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ImpromptuNinjas.ZStd;
using NanoByte.Common.Tasks;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts Zstandard-compressed TAR archives (tar.zst).
    /// </summary>
    public class TarZstandardExtractor : TarExtractor
    {
        /// <summary>
        /// Creates a TAR Zstandard extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public TarZstandardExtractor(ITaskHandler handler)
            : base(handler)
        {}

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
