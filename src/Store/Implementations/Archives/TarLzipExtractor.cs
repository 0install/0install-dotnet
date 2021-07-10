// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common.Tasks;
using SharpCompress.Compressors;
using SharpCompress.Compressors.LZMA;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts Lzip-compressed TAR archives (.tar.lz).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class TarLzipExtractor : TarExtractor
    {
        /// <summary>
        /// Creates a TAR Lzip extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public TarLzipExtractor(ITaskHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
            => base.Extract(builder, new LZipStream(stream, CompressionMode.Decompress), subDir);
    }
}
