// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using NanoByte.Common.Tasks;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts BZip2-compressed TAR archives (.tar.bz2).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class TarBz2Extractor : TarExtractor
    {
        /// <summary>
        /// Creates a TAR BZip2 extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public TarBz2Extractor(ITaskHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        {
            try
            {
                base.Extract(builder, new BZip2InputStream(stream) {IsStreamOwner = false}, subDir);
            }
            #region Error handling
            catch (BZip2Exception ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }
    }
}
