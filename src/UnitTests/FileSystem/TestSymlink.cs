// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;

namespace ZeroInstall.FileSystem
{
    /// <summary>
    /// Represents a symlink used for testing file system operations.
    /// It can either be realized on-disk or compared against an existing on-disk symlink.
    /// </summary>
    /// <seealso cref="TestRoot"/>
    public class TestSymlink : TestElement
    {
        /// <summary>
        /// The path the symlink points to relative to its own location.
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// Creates a new test symlink.
        /// </summary>
        /// <param name="name">The name of the symlink.</param>
        /// <param name="target">The path the symlink points to relative to its own location.</param>
        public TestSymlink(string name, string target)
            : base(name)
        {
            Target = target;
        }

        public override void Build(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            if (UnixUtils.IsUnix) FileUtils.CreateSymlink(path, Target);
            else CygwinUtils.CreateSymlink(path, Target);
        }

        public override void Verify(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            (File.Exists(path) || Directory.Exists(path)).Should().BeTrue(because: $"Symlink '{path}' should exist.");

            bool isSymlink = UnixUtils.IsUnix
                ? FileUtils.IsSymlink(path, out string? target)
                : CygwinUtils.IsSymlink(path, out target);
            isSymlink.Should().BeTrue(because: $"'{path}' should be a symlink.");
            target.Should().Be(Target, because: $"Symlink '{path}' should point to correct target.");
        }
    }
}
