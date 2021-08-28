// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish
{
    public class BuilderExtensionsTest
    {
        private const string SingleFileData = "data";
        private const string SingleFileName = "file.dat";

        private readonly SimpleCommandExecutor _executor = new();
        private readonly SilentTaskHandler _handler = new();
        private readonly ManifestBuilder _dummyBuilder = new(ManifestFormat.Sha1New);

        [Fact]
        public void AddArchive()
        {
            using var stream = typeof(RetrievalMethodExtensionsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var archive = new Archive {Href = microServer.FileUri};
            _dummyBuilder.Add(archive, _executor, _handler);

            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }

        [Fact]
        public void AddSingleFile()
        {
            using var stream = SingleFileData.ToStream();
            using var microServer = new MicroServer(SingleFileName, stream);
            var file = new SingleFile {Href = microServer.FileUri, Destination = SingleFileName};
            _dummyBuilder.Add(file, _executor, _handler);

            file.Size.Should().Be(stream.Length);
        }

        [Fact]
        public void AddRecipe()
        {
            using var stream = typeof(RetrievalMethodExtensionsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var archive = new Archive {Href = microServer.FileUri};
            var recipe = new Recipe {Steps = {archive}};
            _dummyBuilder.Add(recipe, _executor, _handler);

            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }
    }
}
