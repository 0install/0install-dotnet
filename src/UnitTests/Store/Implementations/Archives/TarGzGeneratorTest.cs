// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Contains test methods for <see cref="TarGzGenerator"/>.
    /// </summary>
    public class TarGzGeneratorTest : TarGeneratorTest
    {
        protected override TarGenerator CreateGenerator(string sourceDirectory, Stream stream) => new TarGzGenerator(sourceDirectory, stream);

        protected override Stream BuildArchive(string sourcePath) => TarGzExtractor.GetDecompressionStream(base.BuildArchive(sourcePath));
    }
}
