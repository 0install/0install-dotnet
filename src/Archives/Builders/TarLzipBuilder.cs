// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Compressors;
using SharpCompress.Compressors.LZMA;

namespace ZeroInstall.Archives.Builders
{
    /// <summary>
    /// Builds a Lzip-compressed TAR archive (.tar.lz).
    /// </summary>
    public class TarLzipBuilder : TarBuilder
    {
        /// <summary>
        /// Creates a TAR Lzip archive builder.
        /// </summary>
        /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
        public TarLzipBuilder(Stream stream)
            : base(new LZipStream(stream, CompressionMode.Compress))
        {}
    }
}
