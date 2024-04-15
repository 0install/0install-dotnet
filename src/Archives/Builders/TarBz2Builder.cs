// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ICSharpCode.SharpZipLib.BZip2;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a BZip2-compressed TAR archive (.tar.bz2).
/// </summary>
/// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
/// <param name="fast">The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</param>
[MustDisposeResource]
public class TarBz2Builder(Stream stream, bool fast = false) : TarBuilder(new BZip2OutputStream(stream, blockSize: fast ? 1 : 9));
