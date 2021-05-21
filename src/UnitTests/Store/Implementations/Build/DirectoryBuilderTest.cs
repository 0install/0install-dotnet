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
    /// Contains test methods for <see cref="DirectoryBuilder"/>.
    /// </summary>
    public class DirectoryBuilderTest : IDisposable
    {
        private readonly TemporaryDirectory _tempDir;
        private readonly DirectoryBuilder _builder;

        public DirectoryBuilderTest()
        {
            _tempDir = new TemporaryDirectory("0install-test-builder");
            _builder = new DirectoryBuilder(_tempDir);
        }

        public void Dispose() => _tempDir.Dispose();

        [Fact]
        public void Basic()
        {
            _builder.EnsureDirectory();
            _builder.CreateDirectory("dir", TestFile.DefaultLastWrite);
            File.WriteAllText(_builder.NewFilePath("dir/file", TestFile.DefaultLastWrite), TestFile.DefaultContents);
            _builder.QueueHardlink("dir/hardlink", "dir/file");
            File.WriteAllText(_builder.NewFilePath("dir/executable", TestFile.DefaultLastWrite, executable: true), TestFile.DefaultContents);
            _builder.CreateSymlink("dir/symlink", "target");
            _builder.CompletePending();

            new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestFile("file"),
                    new TestFile("hardlink"),
                    new TestFile("executable") {IsExecutable = true},
                    new TestSymlink("symlink", "target")
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void Suffix()
        {
            _builder.TargetSuffix = "suffix";
            _builder.EnsureDirectory();
            _builder.CreateDirectory("dir", TestFile.DefaultLastWrite);
            File.WriteAllText(_builder.NewFilePath("dir/file", TestFile.DefaultLastWrite), TestFile.DefaultContents);
            _builder.QueueHardlink("dir/hardlink", "dir/file");
            File.WriteAllText(_builder.NewFilePath("dir/executable", TestFile.DefaultLastWrite, executable: true), TestFile.DefaultContents);
            _builder.CreateSymlink("dir/symlink", "target");
            _builder.CompletePending();

            new TestRoot
            {
                new TestDirectory("suffix")
                {
                    new TestDirectory("dir")
                    {
                        new TestFile("file"),
                        new TestFile("hardlink"),
                        new TestFile("executable") {IsExecutable = true},
                        new TestSymlink("symlink", "target")
                    }
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void OverwriteFile()
        {
            new TestRoot {new TestFile("file") {LastWrite = new DateTime(2000, 2, 2), Contents = "wrong", IsExecutable = true}}.Build(_tempDir);

            _builder.EnsureDirectory();
            File.WriteAllText(_builder.NewFilePath("file", TestFile.DefaultLastWrite), TestFile.DefaultContents);
            _builder.CompletePending();

            new TestRoot
            {
                new TestFile("file")
            }.Verify(_tempDir);
        }

        [Fact]
        public void OverwriteSymlink()
        {
            new TestRoot {new TestSymlink("file", "target")}.Build(_tempDir);

            _builder.EnsureDirectory();
            File.WriteAllText(_builder.NewFilePath("file", TestFile.DefaultLastWrite), TestFile.DefaultContents);
            _builder.CompletePending();

            new TestRoot
            {
                new TestFile("file")
            }.Verify(_tempDir);
        }

        [Fact]
        public void OverwriteWithSymlink()
        {
            new TestRoot {new TestFile("file")}.Build(_tempDir);

            _builder.EnsureDirectory();
            _builder.CreateSymlink("file", "target");
            _builder.CompletePending();

            new TestRoot
            {
                new TestSymlink("file", "target")
            }.Verify(_tempDir);
        }

        [Fact]
        public void OverwriteWithHardlink()
        {
            _builder.EnsureDirectory();
            File.WriteAllText(_builder.NewFilePath("file1", TestFile.DefaultLastWrite), TestFile.DefaultContents);
            File.WriteAllText(_builder.NewFilePath("file2", TestFile.DefaultLastWrite), "wrong content");
            _builder.QueueHardlink(source: "file2", target: "file1", executable: true);
            _builder.CompletePending();

            new TestRoot
            {
                new TestFile("file1"),
                new TestFile("file2") {IsExecutable = true}
            }.Verify(_tempDir);
        }
    }
}
