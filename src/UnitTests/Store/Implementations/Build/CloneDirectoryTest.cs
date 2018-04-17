// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Contains test methods for <see cref="CloneDirectory"/>.
    /// </summary>
    public class CloneDirectoryTest : CloneTestBase
    {
        [Fact]
        public void Copy()
        {
            var root = new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestFile("file"),
                    new TestFile("executable") {IsExecutable = true},
                    new TestSymlink("symlink", "target")
                }
            };
            root.Build(SourceDirectory);

            new CloneDirectory(SourceDirectory, TargetDirectory).Run();

            root.Verify(TargetDirectory);
        }

        [Fact]
        public void CopySuffix()
        {
            var root = new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestFile("file"),
                    new TestFile("executable") {IsExecutable = true},
                    new TestSymlink("symlink", "target")
                }
            };
            root.Build(SourceDirectory);

            new CloneDirectory(SourceDirectory, TargetDirectory) {TargetSuffix = "suffix"}.Run();

            root.Verify(Path.Combine(TargetDirectory, "suffix"));
        }

        [Fact]
        public void Hardlink()
        {
            var root = new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestFile("file"),
                    new TestFile("executable") {IsExecutable = true},
                    new TestSymlink("symlink", "target")
                }
            };
            root.Build(SourceDirectory);

            FileUtils.EnableWriteProtection(SourceDirectory); // Hardlinking logic should work around write-protection by temporarily removing it
            try
            {
                new CloneDirectory(SourceDirectory, TargetDirectory) {UseHardlinks = true}.Run();
            }
            finally
            {
                FileUtils.DisableWriteProtection(SourceDirectory);
            }

            root.Verify(TargetDirectory);
            FileUtils.AreHardlinked(Path.Combine(SourceDirectory, "dir", "file"), Path.Combine(TargetDirectory, "dir", "file"));
            FileUtils.AreHardlinked(Path.Combine(SourceDirectory, "dir", "executable"), Path.Combine(TargetDirectory, "dir", "executable"));
        }

        [Fact]
        public void OverwriteFile()
        {
            var root = new TestRoot {new TestFile("fileA")};
            root.Build(SourceDirectory);
            new TestRoot {new TestFile("fileB") {LastWrite = new DateTime(2000, 2, 2), Contents = "wrong", IsExecutable = true}}.Build(TargetDirectory);

            new CloneDirectory(SourceDirectory, TargetDirectory).Run();

            root.Verify(TargetDirectory);
        }

        [Fact]
        public void OverwriteSymlink()
        {
            var root = new TestRoot {new TestFile("fileA")};
            root.Build(SourceDirectory);
            new TestRoot {new TestSymlink("fileB", "target")}.Build(TargetDirectory);

            new CloneDirectory(SourceDirectory, TargetDirectory).Run();

            root.Verify(TargetDirectory);
        }

        [Fact]
        public void OverwriteWithSymlink()
        {
            var root = new TestRoot {new TestSymlink("fileA", "target")};
            root.Build(SourceDirectory);
            new TestRoot {new TestFile("fileB")}.Build(TargetDirectory);

            new CloneDirectory(SourceDirectory, TargetDirectory).Run();

            root.Verify(TargetDirectory);
        }
    }
}
