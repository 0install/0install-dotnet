// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using System.IO;
using Microsoft.Deployment.Compression.Cab;
using NanoByte.Common.Native;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts MS Cabinets (.cab).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class CabExtractor : ArchiveExtractor
    {
        /// <summary>
        /// Creates a CAB extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        /// <exception cref="NotSupportedException">Extracting this archive type is only supported on Windows.</exception>
        public CabExtractor(ITaskHandler handler)
            : base(handler)
        {
            if (!WindowsUtils.IsWindows) throw new NotSupportedException(Resources.ExtractionOnlyOnWindows);
        }

        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        {
            using var engine = new CabEngine();

            EnsureSeekable(stream, seekableStream =>
            {
                try
                {
                    engine.Unpack(
                        new CabExtractorContext(builder, seekableStream, path => NormalizePath(path, subDir), Handler.CancellationToken),
                        fileFilter: path => NormalizePath(path, subDir) != null);
                }
                #region Error handling
                catch (CabException ex)
                {
                    // Wrap exception since only certain exception types are allowed
                    throw new IOException(Resources.ArchiveInvalid, ex);
                }
                #endregion
            });
        }
    }
}
#endif
