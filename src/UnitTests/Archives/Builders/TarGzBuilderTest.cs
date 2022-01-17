// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using ICSharpCode.SharpZipLib.GZip;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Contains test methods for <see cref="TarGzBuilder"/>.
/// </summary>
public class TarGzBuilderTest : TarBuilderTest
{
    protected override IArchiveBuilder NewBuilder(Stream stream) => new TarGzBuilder(stream);

    protected override Stream GetArchiveStream() => new GZipInputStream(base.GetArchiveStream());
}
