// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NETSTANDARD2_0
using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Deployment.Compression.Cab;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a MS Cabinet archive.
    /// </summary>
    public class CabExtractor : MicrosoftExtractor
    {
        /// <summary>
        /// Prepares to extract a MS Cabinet archive contained in a stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal CabExtractor([NotNull] Stream stream, [NotNull] string targetPath)
            : base(targetPath)
        {
            CabStream = stream ?? throw new ArgumentNullException(nameof(stream));

            try
            {
                UnitsTotal = CabEngine.GetFileInfo(this, _ => true).Sum(x => x.Length);
            }
            #region Error handling
            catch (CabException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        /// <inheritdoc/>
        protected override void ExtractArchive()
        {
            try
            {
                CabEngine.Unpack(this, _ => true);
            }
            #region Error handling
            catch (CabException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }
    }
}
#endif
