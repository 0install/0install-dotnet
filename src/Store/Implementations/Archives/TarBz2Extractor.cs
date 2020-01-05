// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a BZip2-compressed TAR archive.
    /// </summary>
    public class TarBz2Extractor : TarExtractor
    {
        private readonly Stream _stream;

        /// <summary>
        /// Prepares to extract a TAR archive contained in a BZip2-compressed stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal TarBz2Extractor(Stream stream, string targetPath)
            : base(GetDecompressionStream(stream), targetPath)
        {
            _stream = stream;
            UnitsTotal = stream.Length;
        }

        /// <summary>
        /// Adds a BZip2-extraction layer around a stream.
        /// </summary>
        /// <param name="stream">The stream containing the BZip2-compressed data.</param>
        /// <returns>A stream representing the uncompressed data.</returns>
        /// <exception cref="IOException">The compressed stream contains invalid data.</exception>
        internal static Stream GetDecompressionStream(Stream stream)
        {
            try
            {
                return new BZip2InputStream(stream);
            }
            #region Error handling
            catch (BZip2Exception ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        /// <inheritdoc/>
        protected override void UpdateProgress()
            => UnitsProcessed = _stream.Position; // Use original stream instead of decompressed stream to track progress
    }
}
