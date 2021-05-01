// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Creates a BZip2-compressed TAR archive from a directory. Preserves executable bits, symlinks, hardlinks and timestamps.
    /// </summary>
    public class TarBz2Generator : TarGenerator
    {
        internal TarBz2Generator(string sourcePath, Stream stream)
            : base(sourcePath, new BZip2OutputStream(stream))
        {}
    }
}
