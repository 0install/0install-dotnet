// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using JetBrains.Annotations;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Creates a GZip-compressed TAR archive from a directory. Preserves execuable bits, symlinks, hardlinks and timestamps.
    /// </summary>
    public class TarBz2Generator : TarGenerator
    {
        internal TarBz2Generator([NotNull] string sourcePath, [NotNull] Stream stream)
            : base(sourcePath, new BZip2OutputStream(stream))
        {}
    }
}
