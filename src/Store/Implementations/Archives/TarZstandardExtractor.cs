// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ImpromptuNinjas.ZStd;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a Zstandard-compressed TAR archive.
    /// </summary>
    public class TarZstandardExtractor : TarExtractor
    {
        private readonly Stream _stream;

        /// <summary>
        /// Prepares to extract a TAR archive contained in a Zstandard-compressed stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal TarZstandardExtractor(Stream stream, string targetPath)
            : base(new ZStdDecompressStream(stream), targetPath)
        {
            _stream = stream;
            UnitsTotal = stream.Length;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            try
            {
                base.Dispose();
            }
            finally
            {
                // ZStdDecompressStream does not automatically dispose the inner stream so we need to do it here ourselves
                _stream.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override void UpdateProgress()
            => UnitsProcessed = _stream.Position; // Use original stream instead of decompressed stream to track progress
    }
}
