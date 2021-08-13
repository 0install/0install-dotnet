// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace ZeroInstall.Archives.Builders
{
    /// <summary>
    /// Builds a BZip2-compressed TAR archive (.tar.bz2).
    /// </summary>
    public class TarBz2Builder : TarBuilder
    {
        /// <summary>
        /// Creates a TAR BZip2 archive builder.
        /// </summary>
        /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
        public TarBz2Builder(Stream stream)
            : base(new BZip2OutputStream(stream))
        {}
    }
}
