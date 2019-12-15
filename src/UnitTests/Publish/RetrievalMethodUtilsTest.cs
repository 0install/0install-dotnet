// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using Xunit;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Contains test methods for <see cref="RetrievalMethodUtils"/>.
    /// </summary>
    public class RetrievalMethodUtilsTest
    {
        private const string SingleFileData = "data";
        private const string SingleFileName = "file.dat";

        /// <summary>
        /// Ensures <see cref="RetrievalMethodUtils.DownloadAndApply(DownloadRetrievalMethod,ITaskHandler,ICommandExecutor)"/> works correctly with <see cref="Archive"/>s.
        /// </summary>
        [Fact]
        public void DownloadAndApplyArchive()
        {
            using var stream = typeof(RetrievalMethodUtilsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var archive = new Archive {Href = microServer.FileUri};
            archive.DownloadAndApply(new SilentTaskHandler()).Dispose();

            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }

        /// <summary>
        /// Ensures <see cref="RetrievalMethodUtils.DownloadAndApply(DownloadRetrievalMethod,ITaskHandler,ICommandExecutor)"/> works correctly with <see cref="SingleFile"/>s.
        /// </summary>
        [Fact]
        public void DownloadAndApplySingleFile()
        {
            using var stream = SingleFileData.ToStream();
            using var microServer = new MicroServer(SingleFileName, stream);
            var file = new SingleFile {Href = microServer.FileUri, Destination = SingleFileName};
            file.DownloadAndApply(new SilentTaskHandler()).Dispose();

            file.Size.Should().Be(stream.Length);
        }

        /// <summary>
        /// Ensures <see cref="RetrievalMethodUtils.DownloadAndApply(Recipe,ITaskHandler,ICommandExecutor)"/> works correctly with <see cref="Recipe"/>s.
        /// </summary>
        [Fact]
        public void DownloadAndApplyRecipe()
        {
            using var stream = typeof(RetrievalMethodUtilsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var archive = new Archive {Href = microServer.FileUri};
            var recipe = new Recipe {Steps = {archive}};
            recipe.DownloadAndApply(new SilentTaskHandler()).Dispose();

            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }

        /// <summary>
        /// Ensures <see cref="RetrievalMethodUtils.LocalApply"/> handles <see cref="Archive"/>s without downloading them.
        /// </summary>
        [Fact]
        public void LocalApplyArchive()
        {
            using var tempDir = new TemporaryDirectory("0install-unit-tests");
            string tempFile = Path.Combine(tempDir, "archive.zip");
            typeof(RetrievalMethodUtilsTest).CopyEmbeddedToFile("testArchive.zip", tempFile);

            var archive = new Archive();
            using (var extractedDir = archive.LocalApply(tempFile, new SilentTaskHandler()))
                File.Exists(Path.Combine(extractedDir, "symlink")).Should().BeTrue();

            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(new FileInfo(tempFile).Length);

            File.Exists(tempFile).Should().BeTrue(because: "Local reference file should not be removed");
        }

        /// <summary>
        /// Ensures <see cref="RetrievalMethodUtils.LocalApply"/> handles <see cref="SingleFile"/>s without downloading them.
        /// </summary>
        [Fact]
        public void LocalApplySingleFile()
        {
            using var tempDir = new TemporaryDirectory("0install-unit-tests");
            string tempFile = Path.Combine(tempDir, "file");
            File.WriteAllText(tempFile, @"abc");

            var file = new SingleFile();
            using (var extractedDir = file.LocalApply(tempFile, new SilentTaskHandler()))
                File.Exists(Path.Combine(extractedDir, "file")).Should().BeTrue();

            file.Destination.Should().Be("file");
            file.Size.Should().Be(3);

            File.Exists(tempFile).Should().BeTrue(because: "Local reference file should not be removed");
        }
    }
}
