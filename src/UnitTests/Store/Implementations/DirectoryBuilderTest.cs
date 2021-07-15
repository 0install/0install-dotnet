// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations
{
    public class DirectoryBuilderTest : IDisposable
    {
        private const string Data = "data";
        private static Stream DataStream => Data.ToStream();

        private readonly TemporaryDirectory _tempDir = new("0install-unit-test-impl");
        private readonly DirectoryBuilder _builder;

        public DirectoryBuilderTest()
        {
            _builder = new DirectoryBuilder(_tempDir, new ManifestBuilder(ManifestFormat.Sha1New));
        }

        public void Dispose() => _tempDir.Dispose();

        [Fact]
        public void AddFile()
        {
            _builder.AddFile("file", DataStream, 1337);

            new TestRoot
            {
                new TestFile("file") {Contents = Data, LastWrite = 1337}
            }.Verify(_tempDir);
        }

        [Fact]
        public void OverwriteFile()
        {
            _builder.AddFile("file", "dummy".ToStream(), 42);
            _builder.AddFile("file", DataStream, 1337, executable: true);

            new TestRoot
            {
                new TestFile("file") {Contents = Data, LastWrite = 1337, IsExecutable = true}
            }.Verify(_tempDir);
        }

        [Fact]
        public void MarkAsExecutable()
        {
            _builder.AddFile("file", DataStream, 1337);
            _builder.MarkAsExecutable("file");

            new TestRoot
            {
                new TestFile("file") {Contents = Data, LastWrite = 1337, IsExecutable = true}
            }.Verify(_tempDir);
        }

        [Fact]
        public void RenameFile()
        {
            _builder.AddFile("file", DataStream, 1337, executable: true);
            _builder.Rename("file", "file2");

            new TestRoot
            {
                new TestFile("file2") {Contents = Data, LastWrite = 1337, IsExecutable = true}
            }.Verify(_tempDir);
        }

        [Fact]
        public void RenameFileMissing()
        {
            _builder.Invoking(x => x.Rename("file", "file2"))
                    .Should().Throw<IOException>();
        }

        [Fact]
        public void RemoveFile()
        {
            _builder.AddFile("file", DataStream, 1337);
            _builder.AddFile("file2", DataStream, 1337);
            _builder.Remove("file");

            new TestRoot
            {
                new TestFile("file2") {Contents = Data, LastWrite = 1337}
            }.Verify(_tempDir);
        }

        [Fact]
        public void RemoveFileMissing()
        {
            _builder.Invoking(x => x.Remove("file"))
                    .Should().Throw<IOException>();
        }

        [Fact]
        public void AddDirectory()
        {
            _builder.AddDirectory("dir");

            new TestRoot
            {
                new TestDirectory("dir")
            }.Verify(_tempDir);
        }

        [Fact]
        public void AddDirectoryAndFile()
        {
            // Implicit: _builder.AddDirectory("dir");
            _builder.AddFile(Path.Combine("dir", "file"), DataStream, 1337);

            new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestFile("file") {Contents = Data, LastWrite = 1337}
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void RenameDirectory()
        {
            _builder.AddDirectory("dir");
            _builder.AddFile(Path.Combine("dir", "file"), DataStream, 1337);
            _builder.Rename("dir", "dir2");

            new TestRoot
            {
                new TestDirectory("dir2")
                {
                    new TestFile("file") {Contents = Data, LastWrite = 1337}
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void RemoveDirectory()
        {
            _builder.AddDirectory("dir");
            _builder.AddFile(Path.Combine("dir", "file"), DataStream, 1337);
            _builder.AddDirectory("dir2");
            _builder.AddFile(Path.Combine("dir2", "file"), DataStream, 1337);
            _builder.Remove("dir");

            new TestRoot
            {
                new TestDirectory("dir2")
                {
                    new TestFile("file") {Contents = Data, LastWrite = 1337}
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void AddHardLink()
        {
            _builder.AddFile(Path.Combine("dir", "file"), DataStream, 1337);
            _builder.AddHardlink(Path.Combine("dir", "file2"), Path.Combine("dir", "file"));

            new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestFile("file") {Contents = Data, LastWrite = 1337},
                    new TestFile("file2") {Contents = Data, LastWrite = 1337}
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void AddHardLinkMissing()
        {
            _builder.Invoking(x => x.AddHardlink("file2", "file"))
                    .Should().Throw<IOException>();
        }

        [Fact]
        public void AddSymlink()
        {
            _builder.AddSymlink(Path.Combine("dir", "symlink"), "target");

            new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestSymlink("symlink", "target")
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void TurnIntoSymlink()
        {
            _builder.AddFile(Path.Combine("dir", "symlink"), "target".ToStream(), 0);
            _builder.TurnIntoSymlink(Path.Combine("dir", "symlink"));

            new TestRoot
            {
                new TestDirectory("dir")
                {
                    new TestSymlink("symlink", "target")
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void Complex()
        {
            _builder.AddDirectory(Path.Combine("some", "dir"));
            _builder.AddFile(Path.Combine("some", "dir", "file"), DataStream, 1337);
            _builder.Rename(Path.Combine("some", "dir", "file"), Path.Combine("some", "dir", "file1"));
            _builder.AddHardlink(Path.Combine("some", "dir", "file2"), Path.Combine("some", "dir", "file1"));
            _builder.Rename("some", "the");

            new TestRoot
            {
                new TestDirectory("the")
                {
                    new TestDirectory("dir")
                    {
                        new TestFile("file1") {Contents = Data, LastWrite = 1337},
                        new TestFile("file2") {Contents = Data, LastWrite = 1337}
                    }
                }
            }.Verify(_tempDir);
        }

        [Fact]
        public void RejectsInvalidPaths()
        {
            _builder.Invoking(x => x.AddFile("a\nb", DataStream, 0))
                    .Should().Throw<IOException>();
            _builder.Invoking(x => x.AddFile(".manifest", DataStream, 0))
                    .Should().Throw<IOException>();
            _builder.Invoking(x => x.AddFile(".xbit", DataStream, 0))
                    .Should().Throw<IOException>();
        }
    }
}
