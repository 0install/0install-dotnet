// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Compressors;
using SharpCompress.Compressors.LZMA;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a Lzip-compressed TAR archive.
    /// </summary>
    public class TarLzipExtractor : TarExtractor
    {
        private readonly Stream _stream;

        /// <summary>
        /// Prepares to extract a TAR archive contained in a Lzip-compressed stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal TarLzipExtractor(Stream stream, string targetPath)
            : base(new LZipStream(stream, CompressionMode.Decompress), targetPath)
        {
            _stream = stream;
            UnitsTotal = stream.Length;
        }

        /// <inheritdoc/>
        protected override void UpdateProgress()
            => UnitsProcessed = _stream.Position; // Use original stream instead of decompressed stream to track progress
    }
}
