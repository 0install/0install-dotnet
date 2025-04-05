// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using SharpCompress.Compressors;
using SharpCompress.Compressors.LZMA;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a Lzip-compressed TAR archive (.tar.lz).
/// </summary>
/// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
[MustDisposeResource]
public class TarLzipBuilder(Stream stream) : TarBuilder(new LZipStream(stream, CompressionMode.Compress));
#endif
