// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common.Tasks;
using SharpCompress.Compressors.Xz;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts XZ-compressed TAR archives (tar.xz).
    /// </summary>
    public class TarXzExtractor : TarExtractor
    {
        /// <summary>
        /// Creates a TAR XZ extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public TarXzExtractor(ITaskHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
            => base.Extract(builder, new XZStream(stream), subDir);
    }
}
