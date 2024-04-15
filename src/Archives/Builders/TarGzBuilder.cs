// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Compression;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a GZip-compressed TAR archive (.tar.gz).
/// </summary>
/// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
/// <param name="fast">The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</param>
[MustDisposeResource]
public class TarGzBuilder(Stream stream, bool fast = false) : TarBuilder(new GZipStream(stream, fast
        ? CompressionLevel.Fastest
#if NET
        : CompressionLevel.SmallestSize
#else
        : CompressionLevel.Optimal
#endif
));
