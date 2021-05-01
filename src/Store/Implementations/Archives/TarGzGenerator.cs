// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.GZip;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Creates a GZip-compressed TAR archive from a directory. Preserves executable bits, symlinks, hardlinks and timestamps.
    /// </summary>
    public class TarGzGenerator : TarGenerator
    {
        internal TarGzGenerator(string sourcePath, Stream stream)
            : base(sourcePath, new GZipOutputStream(stream))
        {}
    }
}
