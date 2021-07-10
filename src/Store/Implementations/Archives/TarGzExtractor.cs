// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts GZip-compressed TAR archives (.tar.gz).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class TarGzExtractor : TarExtractor
    {
        /// <summary>
        /// Creates a TAR GZip extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public TarGzExtractor(ITaskHandler handler)
            : base(handler)
        {}

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
