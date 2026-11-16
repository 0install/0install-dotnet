// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL

#if NET11_0_OR_GREATER
using System.IO.Compression;
#else
using ZstdSharp;
#endif

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a Zstandard-compressed TAR archive (tar.zst).
/// </summary>
/// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
/// <param name="fast">The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</param>
[MustDisposeResource]
public class TarZstandardBuilder(Stream stream, bool fast = false) : TarBuilder(
#if NET11_0_OR_GREATER
    new ZstandardStream(stream, new ZstandardCompressionOptions {Quality = fast ? 3 : 19}, leaveOpen: true)
#else
    new CompressionStream(stream, level: fast ? 3 : 19)
#endif
)
{
    public override void Dispose()
    {
        try
        {
            base.Dispose();
        }
        finally
        {
            stream.Dispose();
        }
    }
}
#endif
