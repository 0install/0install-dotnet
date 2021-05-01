// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;

namespace ZeroInstall.FileSystem
{
    /// <summary>
    /// Represents a non-existing/deleted file used for testing file system operations.
    /// It can either be deleted from disk or compared against the existence of an on-disk file.
    /// </summary>
    /// <seealso cref="TestRoot"/>
    public class TestDeletedFile : TestElement
    {
        /// <summary>
        /// Creates a new test deleted file.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        public TestDeletedFile(string name)
            : base(name)
        {}

        public override void Build(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            File.Delete(path);
        }

        public override void Verify(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            File.Exists(path).Should().BeFalse(because: $"File '{path}' should not exist.");
        }
    }
}
