// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using ZstdSharp;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a Zstandard-compressed TAR archive (tar.zst).
/// </summary>
/// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
/// <param name="fast">The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</param>
[MustDisposeResource]
public class TarZstandardBuilder(Stream stream, bool fast = false) : TarBuilder(new CompressionStream(stream, level: fast ? 3 : 19))
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
