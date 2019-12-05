// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System.IO;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Contains test methods for <see cref="TarLzmaGenerator"/>.
    /// </summary>
    public class TarLzmaGeneratorTest : TarGeneratorTest
    {
        protected override TarGenerator CreateGenerator(string sourceDirectory, Stream stream) => new TarLzmaGenerator(sourceDirectory, stream);

        protected override Stream BuildArchive(string sourcePath) => TarLzmaExtractor.GetDecompressionStream(base.BuildArchive(sourcePath));
    }
}
#endif
