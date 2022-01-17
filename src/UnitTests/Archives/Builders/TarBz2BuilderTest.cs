// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Contains test methods for <see cref="TarBz2Builder"/>.
/// </summary>
public class TarBz2BuilderTest : TarBuilderTest
{
    protected override IArchiveBuilder NewBuilder(Stream stream) => new TarBz2Builder(stream);

    protected override Stream GetArchiveStream() => new BZip2InputStream(base.GetArchiveStream());
}
