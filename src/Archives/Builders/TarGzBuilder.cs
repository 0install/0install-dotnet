// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Compression;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a GZip-compressed TAR archive (.tar.gz).
/// </summary>
public class TarGzBuilder : TarBuilder
{
    /// <summary>
    /// Creates a TAR GZip archive builder.
    /// </summary>
    /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
    public TarGzBuilder(Stream stream)
        : base(new GZipStream(stream, CompressionMode.Compress))
    {}
}
