// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Contains test methods for <see cref="ImplementationUtils"/>.
    /// </summary>
    [Collection("Static state")]
    public class ImplementationUtilsTest
    {
        private const string ArchiveSha256Digest = "TPD62FAK7ME7OCER5CHL3HQDZQMNJVENJUBL6E6IXX5UI44OXMJQ";

        private const string SingleFileData = "data";
        private const string SingleFileName = "file.dat";

        private static readonly string _singleFileSha256Digest = new Manifest(ManifestFormat.Sha256New)
        {
            [""] =
            {
                [SingleFileName] = new ManifestNormalFile(SingleFileData.Hash(SHA256.Create()), 0, SingleFileData.Length)
            }
        }.CalculateDigest();

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.Build"/> works correctly with <see cref="Archive"/>s.
        /// </summary>
        [Fact]
        public void BuildArchive()
        {
            using var stream = typeof(ImplementationUtilsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var implementation = ImplementationUtils.Build(new Archive {Href = microServer.FileUri}, new SilentTaskHandler());
            implementation.ManifestDigest.Sha256New.Should().Be(ArchiveSha256Digest);

            var archive = (Archive)implementation.RetrievalMethods[0];
            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.Build"/> works correctly with <see cref="SingleFile"/>s.
        /// </summary>
        [Fact]
        public void BuildSingleFile()
        {
            using var originalStream = SingleFileData.ToStream();
            using var microServer = new MicroServer(SingleFileName, originalStream);
            var implementation = ImplementationUtils.Build(new SingleFile {Href = microServer.FileUri, Destination = SingleFileName}, new SilentTaskHandler());
            ("sha256new_" + implementation.ManifestDigest.Sha256New).Should().Be(_singleFileSha256Digest);

            var file = (SingleFile)implementation.RetrievalMethods[0];
            file.Size.Should().Be(originalStream.Length);
        }

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.Build"/> works correctly with <see cref="Recipe"/>s.
        /// </summary>
        [Fact]
        public void BuildRecipe()
        {
            using var stream = typeof(ImplementationUtilsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var implementation = ImplementationUtils.Build(new Recipe {Steps = {new Archive {Href = microServer.FileUri}}}, new SilentTaskHandler());
            implementation.ManifestDigest.Sha256New.Should().Be(ArchiveSha256Digest);

            var archive = (Archive)((Recipe)implementation.RetrievalMethods[0]).Steps[0];
            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.AddMissing"/> works correctly with <see cref="Archive"/>s.
        /// </summary>
        [Fact]
        public void AddMissingArchive()
        {
            using var stream = typeof(ImplementationUtilsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var implementation = new Implementation {RetrievalMethods = {new Archive {Href = microServer.FileUri}}};
            implementation.AddMissing(new SilentTaskHandler());
            implementation.ManifestDigest.Sha256New.Should().Be(ArchiveSha256Digest);

            var archive = (Archive)implementation.RetrievalMethods[0];
            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.AddMissing"/> works correctly with <see cref="SingleFile"/>s.
        /// </summary>
        [Fact]
        public void AddMissingSingleFile()
        {
            using var originalStream = SingleFileData.ToStream();
            using var microServer = new MicroServer(SingleFileName, originalStream);
            var implementation = new Implementation {RetrievalMethods = {new SingleFile {Href = microServer.FileUri}}};
            implementation.AddMissing(new SilentTaskHandler());
            ("sha256new_" + implementation.ManifestDigest.Sha256New).Should().Be(_singleFileSha256Digest);

            var file = (SingleFile)implementation.RetrievalMethods[0];
            file.Size.Should().Be(originalStream.Length);
            file.Destination.Should().Be(SingleFileName);
        }

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.AddMissing"/> works correctly with <see cref="Recipe"/>s.
        /// </summary>
        [Fact]
        public void AddMissingRecipe()
        {
            using var stream = typeof(ImplementationUtilsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var implementation = new Implementation {RetrievalMethods = {new Recipe {Steps = {new Archive {Href = microServer.FileUri}}}}};
            implementation.AddMissing(new SilentTaskHandler());
            implementation.ManifestDigest.Sha256New.Should().Be(ArchiveSha256Digest);

            var archive = (Archive)((Recipe)implementation.RetrievalMethods[0]).Steps[0];
            archive.MimeType.Should().Be(Archive.MimeTypeZip);
            archive.Size.Should().Be(stream.Length);
        }

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.AddMissing"/> generates missing <see cref="Archive"/>s from <see cref="ImplementationBase.LocalPath"/>s.
        /// </summary>
        [Fact]
        public void GenerateMissingArchive()
        {
            using var tempDir = new TemporaryDirectory("0install-test-missing");
            string feedPath = Path.Combine(tempDir, "feed.xml");
            Directory.CreateDirectory(Path.Combine(tempDir, "impl"));
            FileUtils.Touch(Path.Combine(tempDir, "impl", "file"));

            var archive = new Archive {Href = new("archive.zip", UriKind.Relative)};
            var implementation = new Implementation {LocalPath = "impl", RetrievalMethods = {archive}};

            implementation.AddMissing(new SilentTaskHandler(), new SimpleCommandExecutor {Path = feedPath});

            implementation.LocalPath.Should().BeNull();
            implementation.ManifestDigest.Should().NotBe(default(ManifestDigest));
            archive.Size.Should().NotBe(0);

            File.Exists(Path.Combine(tempDir, "archive.zip")).Should().BeTrue();
        }

        /// <summary>
        /// Ensures <see cref="ImplementationUtils.AddMissing"/> throws <see cref="DigestMismatchException"/>s when appropriate.
        /// </summary>
        [Fact]
        public void AddMissingExceptions()
        {
            using var stream = typeof(ImplementationUtilsTest).GetEmbeddedStream("testArchive.zip");
            using var microServer = new MicroServer("archive.zip", stream);
            var implementation = new Implementation {ManifestDigest = new ManifestDigest(sha1New: "invalid"), RetrievalMethods = {new Archive {Href = microServer.FileUri}}};
            Assert.Throws<DigestMismatchException>(() => implementation.AddMissing(new SilentTaskHandler()));
        }
    }
}
