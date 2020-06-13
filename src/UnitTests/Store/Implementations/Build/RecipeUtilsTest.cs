// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Model;
using ZeroInstall.Services;
using ZeroInstall.Store.Implementations.Archives;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Contains test methods for <see cref="RecipeUtils"/>.
    /// </summary>
    public class RecipeUtilsTest
    {
        [Fact]
        public void TestApplyRecipeArchive()
        {
            using var archiveFile = new TemporaryFile("0install-unit-tests");
            typeof(ArchiveExtractorTest).CopyEmbeddedToFile("testArchive.zip", archiveFile);

            var downloadedFiles = new[] {archiveFile};
            var recipe = new Recipe {Steps = {new Archive {MimeType = Archive.MimeTypeZip, Destination = "dest"}}};

            using var recipeDir = recipe.Apply(downloadedFiles, new SilentTaskHandler());
            new TestRoot
            {
                new TestDirectory("dest")
                {
                    new TestSymlink("symlink", "subdir1/regular"),
                    new TestDirectory("subdir1")
                    {
                        new TestFile("regular") {LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                    },
                    new TestDirectory("subdir2")
                    {
                        new TestFile("executable") {IsExecutable = true, LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                    }
                }
            }.Verify(recipeDir);
        }

        [Fact]
        public void TestApplyRecipeSingleFileOverwrite()
        {
            using var singleFile = new TemporaryFile("0install-unit-tests");
            using var archiveFile = new TemporaryFile("0install-unit-tests");
            File.WriteAllText(singleFile, TestFile.DefaultContents);
            typeof(ArchiveExtractorTest).CopyEmbeddedToFile("testArchive.zip", archiveFile);

            var downloadedFiles = new[] {archiveFile, singleFile};
            var recipe = new Recipe {Steps = {new Archive {MimeType = Archive.MimeTypeZip}, new SingleFile {Destination = "subdir2/executable"}}};

            using var recipeDir = recipe.Apply(downloadedFiles, new SilentTaskHandler());
            new TestRoot
            {
                new TestDirectory("subdir2")
                {
                    new TestFile("executable")
                    {
                        IsExecutable = false, // Executable file was overwritten by a non-executable one
                        LastWrite = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                }
            }.Verify(recipeDir);
        }

        [Fact]
        public void TestApplyRecipeSingleFileExecutable()
        {
            using var singleFile = new TemporaryFile("0install-unit-tests");
            File.WriteAllText(singleFile, TestFile.DefaultContents);
            var recipe = new Recipe {Steps = {new SingleFile {Destination = "executable", Executable = true}}};
            using var recipeDir = recipe.Apply(new[] {singleFile}, new SilentTaskHandler());
            new TestRoot
            {
                new TestFile("executable")
                {
                    IsExecutable = true,
                    LastWrite = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            }.Verify(recipeDir);
        }

        [Fact]
        public void TestApplyRecipeRemove()
        {
            using var archiveFile = new TemporaryFile("0install-unit-tests");
            typeof(ArchiveExtractorTest).CopyEmbeddedToFile("testArchive.zip", archiveFile);

            var downloadedFiles = new[] {archiveFile};
            var recipe = new Recipe
            {
                Steps =
                {
                    new Archive {MimeType = Archive.MimeTypeZip},
                    new RemoveStep {Path = "symlink"},
                    new RemoveStep {Path = "subdir2"}
                }
            };

            using var recipeDir = recipe.Apply(downloadedFiles, new SilentTaskHandler());
            new TestRoot
            {
                new TestDeletedFile("symlink"),
                new TestDirectory("subdir1")
                {
                    new TestFile("regular") {LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                },
                new TestDeletedDirectory("subdir2")
            }.Verify(recipeDir);
        }

        [Fact]
        public void TestApplyRecipeRename()
        {
            using var archiveFile = new TemporaryFile("0install-unit-tests");
            typeof(ArchiveExtractorTest).CopyEmbeddedToFile("testArchive.zip", archiveFile);

            var downloadedFiles = new[] {archiveFile};
            var recipe = new Recipe
            {
                Steps =
                {
                    new Archive {MimeType = Archive.MimeTypeZip},
                    new RenameStep {Source = "symlink", Destination = "subdir3/symlink2"},
                    new RenameStep {Source = "subdir2/executable", Destination = "subdir2/executable2"}
                }
            };

            using var recipeDir = recipe.Apply(downloadedFiles, new SilentTaskHandler());
            new TestRoot
            {
                new TestDeletedFile("symlink"),
                new TestDirectory("subdir1")
                {
                    new TestFile("regular") {LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)},
                },
                new TestDirectory("subdir2")
                {
                    new TestDeletedFile("executable"),
                    new TestFile("executable2") {IsExecutable = true, LastWrite = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)}
                },
                new TestDirectory("subdir3")
                {
                    new TestSymlink("symlink2", "subdir1/regular")
                }
            }.Verify(recipeDir);
        }

        [Fact]
        public void TestApplyRecipeCopyFrom()
        {
            using var existingImplDir = new TemporaryDirectory("0install-unit-tests");
            using var archiveFile = new TemporaryFile("0install-unit-tests");
            new TestRoot
            {
                new TestDirectory("source")
                {
                    new TestFile("file"),
                    new TestFile("executable") {IsExecutable = true},
                    new TestSymlink("symlink", "target")
                }
            }.Build(existingImplDir);

            typeof(ArchiveExtractorTest).CopyEmbeddedToFile("testArchive.zip", archiveFile);

            var downloadedFiles = new[] {archiveFile};
            var recipe = new Recipe
            {
                Steps =
                {
                    new CopyFromStep
                    {
                        Source = "source",
                        Destination = "dest",
                        Implementation = new Implementation
                        {
                            ManifestDigest = new ManifestDigest(sha1New: "123")
                        }
                    },
                    new CopyFromStep
                    {
                        Source = "source/file",
                        Destination = "dest/symlink", // Overwrite existing symlink with regular file
                        Implementation = new Implementation
                        {
                            ManifestDigest = new ManifestDigest(sha1New: "123")
                        }
                    }
                }
            };

            using (FetchHandle.Register(_ => existingImplDir))
            {
                using var recipeDir = recipe.Apply(downloadedFiles, new SilentTaskHandler());
                new TestRoot
                {
                    new TestDirectory("dest")
                    {
                        new TestFile("file"),
                        new TestFile("executable") {IsExecutable = true},
                        new TestFile("symlink")
                    }
                }.Verify(recipeDir);
            }

            FileUtils.DisableWriteProtection(existingImplDir);
            Directory.Delete(existingImplDir, recursive: true);
        }

        [Fact]
        public void TestApplyRecipeExceptions()
        {
            using (var tempArchive = new TemporaryFile("0install-unit-tests"))
            {
                new Recipe {Steps = {new Archive {Destination = "../destination"}}}
                   .Invoking(x => x.Apply(new[] {tempArchive}, new SilentTaskHandler()))
                   .Should().Throw<IOException>(because: "Should reject breakout path in Archive.Destination");
            }

            using (var tempFile = new TemporaryFile("0install-unit-tests"))
            {
                new Recipe {Steps = {new SingleFile {Destination = "../file"}}}
                   .Invoking(x => x.Apply(new[] {tempFile}, new SilentTaskHandler()))
                   .Should().Throw<IOException>(because: "Should reject breakout path in SingleFile.Destination");
            }

            new Recipe {Steps = {new RemoveStep {Path = "../file"}}}
               .Invoking(x => x.Apply(new TemporaryFile[0], new SilentTaskHandler()))
               .Should().Throw<IOException>(because: "Should reject breakout path in RemoveStep.Path");

            new Recipe {Steps = {new RenameStep {Source = "../source", Destination = "destination"}}}
               .Invoking(x => x.Apply(new TemporaryFile[0], new SilentTaskHandler()))
               .Should().Throw<IOException>(because: "Should reject breakout path in RenameStep.Source");
            new Recipe {Steps = {new RenameStep {Source = "source", Destination = "../destination"}}}
               .Invoking(x => x.Apply(new TemporaryFile[0], new SilentTaskHandler()))
               .Should().Throw<IOException>(because: "Should reject breakout path in RenameStep.Destination");

            new Recipe {Steps = {new CopyFromStep {ID = "id123", Destination = "../destination"}}}
               .Invoking(x => x.Apply(new TemporaryFile[0], new SilentTaskHandler()))
               .Should().Throw<IOException>(because: "Should reject breakout path in CopyFromStep.Destination");
        }

        [Fact]
        public void TestApplySingleFilePath()
        {
            using var tempFile = new TemporaryFile("0install-unit-tests");
            using var workingDir = new TemporaryDirectory("0install-unit-tests");
            File.WriteAllText(tempFile, "data");

            new SingleFile {Destination = "file"}.Apply(tempFile.Path, workingDir, new MockTaskHandler());

            File.Exists(tempFile).Should().BeTrue(because: "Files passed in as string paths should be copied");
            File.Exists(Path.Combine(workingDir, "file")).Should().BeTrue();
        }

        [Fact]
        public void TestApplySingleFileTemp()
        {
            using var tempFile = new TemporaryFile("0install-unit-tests");
            using var workingDir = new TemporaryDirectory("0install-unit-tests");
            File.WriteAllText(tempFile, "data");

            new SingleFile {Destination = "file"}.Apply(tempFile, workingDir);

            File.Exists(tempFile).Should().BeFalse(because: "Files passed in as temp objects should be moved");
            File.Exists(Path.Combine(workingDir, "file")).Should().BeTrue();
        }
    }
}
