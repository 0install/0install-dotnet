// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Compressors;
using SharpCompress.Compressors.LZMA;

namespace ZeroInstall.Archives.Builders
{
    /// <summary>
    /// Contains test methods for <see cref="TarLzipBuilder"/>.
    /// </summary>
    public class TarLzipBuilderTest : TarBuilderTest
    {
        protected override IArchiveBuilder NewBuilder(Stream stream) => new TarLzipBuilder(stream);

        protected override Stream GetArchiveStream() => new LZipStream(base.GetArchiveStream(), CompressionMode.Decompress);
    }
}
