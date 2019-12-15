// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common.Storage;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Common test cases for <see cref="ArchiveGenerator"/> sub-classes.
    /// </summary>
    /// <typeparam name="TGenerator">The specific type of <see cref="ArchiveGenerator"/> to test.</typeparam>
    public abstract class ArchiveGeneratorTest<TGenerator>
        where TGenerator : ArchiveGenerator
    {
        protected abstract TGenerator CreateGenerator(string sourceDirectory, Stream stream);

        protected Stream BuildArchive(TestRoot root)
        {
            using var tempDir = new TemporaryDirectory("0install-unit-tests");
            root.Build(tempDir);
            return BuildArchive(tempDir);
        }

        protected virtual Stream BuildArchive(string sourcePath)
        {
            using var archiveWriteStream = new MemoryStream();
            using (var generator = CreateGenerator(sourcePath, archiveWriteStream))
                generator.Run();
            return new MemoryStream(archiveWriteStream.ToArray(), writable: false);
        }
    }
}
