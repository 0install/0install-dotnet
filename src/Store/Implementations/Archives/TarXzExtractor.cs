// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Compressors.Xz;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a XZ-compressed TAR archive.
    /// </summary>
    public class TarXzExtractor : TarExtractor
    {
        private readonly Stream _stream;

        /// <summary>
        /// Prepares to extract a TAR archive contained in a XZ-compressed stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal TarXzExtractor(Stream stream, string targetPath)
            : base(new XZStream(stream), targetPath)
        {
            _stream = stream;
            UnitsTotal = stream.Length;
        }

        /// <inheritdoc/>
        protected override void UpdateProgress()
            => UnitsProcessed = _stream.Position; // Use original stream instead of decompressed stream to track progress

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                // Note: XZStream does not automatically dispose the inner stream so we need to do it here ourselves
                _stream.Dispose();
            }
        }
    }
}
