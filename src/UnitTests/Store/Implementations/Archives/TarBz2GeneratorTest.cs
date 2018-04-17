// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Contains test methods for <see cref="TarBz2Generator"/>.
    /// </summary>
    public class TarBz2GeneratorTest : TarGeneratorTest
    {
        protected override TarGenerator CreateGenerator(string sourceDirectory, Stream stream) => new TarBz2Generator(sourceDirectory, stream);

        protected override Stream BuildArchive(string sourcePath) => TarBz2Extractor.GetDecompressionStream(base.BuildArchive(sourcePath));
    }
}
