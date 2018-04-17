// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using JetBrains.Annotations;

namespace ZeroInstall.FileSystem
{
    /// <summary>
    /// Represents a non-existing/deleted directory used for testing file system operations.
    /// It can either be deleted from disk or compared against the existance of an on-disk directory.
    /// </summary>
    /// <seealso cref="TestRoot"/>
    public class TestDeletedDirectory : TestElement
    {
        /// <summary>
        /// Creates a new test deleted directory.
        /// </summary>
        /// <param name="name">The name of the directory.</param>
        public TestDeletedDirectory([NotNull] string name)
            : base(name)
        {}

        public override void Build(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            Directory.Delete(path, recursive: true);
        }

        public override void Verify(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            Directory.Exists(path).Should().BeFalse(because: $"Directory '{path}' should not exist.");
        }
    }
}
