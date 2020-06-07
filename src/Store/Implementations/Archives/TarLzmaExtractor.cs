// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Streams;
using SharpCompress.Compressors.LZMA;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a LZMA-compressed TAR archive.
    /// </summary>
    public class TarLzmaExtractor : TarExtractor
    {
        /// <summary>
        /// Prepares to extract a TAR archive contained in a LZMA-compressed stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal TarLzmaExtractor(Stream stream, string targetPath)
            : base(GetDecompressionStream(stream, out long unitsTotal), targetPath)
        {
            UnitsTotal = unitsTotal;
        }

        /// <summary>
        /// Adds a LZMA-decompression layer around a stream.
        /// </summary>
        /// <param name="stream">The stream containing the LZMA-compressed data.</param>
        /// <param name="uncompressedLength">The length of the uncompressed data in bytes. -1 if unkonw.</param>
        /// <returns>A stream representing the uncompressed data.</returns>
        /// <exception cref="IOException">The compressed stream contains invalid data.</exception>
        private static Stream GetDecompressionStream(Stream stream, out long uncompressedLength)
        {
            if (stream.CanSeek) stream.Position = 0;

            var header = stream.Read(5);

            var uncompressedLengthData = stream.Read(8);
            if (!BitConverter.IsLittleEndian) Array.Reverse(uncompressedLengthData);
            uncompressedLength = BitConverter.ToInt64(uncompressedLengthData, startIndex: 0);

            return new LzmaStream(header, stream);
        }
    }
}
