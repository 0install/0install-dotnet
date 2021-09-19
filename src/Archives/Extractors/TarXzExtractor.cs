// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Compressors.Xz;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts XZ-compressed TAR archives (tar.xz).
    /// </summary>
    [PrimaryConstructor]
    public partial class TarXzExtractor : TarExtractor
    {
        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
            => base.Extract(builder, new XZStream(stream), subDir);
    }
}