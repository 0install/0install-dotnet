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
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish;

public class ImplementationExtensionsTest
{
    private static readonly ManifestDigest _archiveDigest = new(sha256New: "TPD62FAK7ME7OCER5CHL3HQDZQMNJVENJUBL6E6IXX5UI44OXMJQ");

    private const string SingleFileData = "data";
    private const string SingleFileName = "file.dat";

    private static readonly string _singleFileSha256Digest = new Manifest(ManifestFormat.Sha256New)
    {
        [""] =
        {
            [SingleFileName] = new ManifestNormalFile(SingleFileData.Hash(SHA256.Create()), 0, SingleFileData.Length)
        }
    }.CalculateDigest();

    [Fact]
    public void SetMissingArchive()
    {
        using var stream = typeof(ImplementationExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);
        var implementation = new Implementation {RetrievalMethods = {new Archive {Href = microServer.FileUri}}};
        implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler());
        implementation.ManifestDigest.Should().Be(_archiveDigest);

        var archive = (Archive)implementation.RetrievalMethods[0];
        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(stream.Length);
    }

    [Fact]
    public void SetMissingSingleFile()
    {
        using var originalStream = SingleFileData.ToStream();
        using var microServer = new MicroServer(SingleFileName, originalStream);
        var implementation = new Implementation {RetrievalMethods = {new SingleFile {Href = microServer.FileUri, Destination = SingleFileName}}};
        implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler());
        ("sha256new_" + implementation.ManifestDigest.Sha256New).Should().Be(_singleFileSha256Digest);

        var file = (SingleFile)implementation.RetrievalMethods[0];
        file.Size.Should().Be(originalStream.Length);
        file.Destination.Should().Be(SingleFileName);
    }

    [Fact]
    public void SetMissingRecipe()
    {
        using var stream = typeof(ImplementationExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);
        var archive = new Archive {Href = microServer.FileUri};
        var implementation = new Implementation {RetrievalMethods = {new Recipe {Steps = {archive}}}};
        implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler());
        implementation.ManifestDigest.Should().Be(_archiveDigest);

        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(stream.Length);
    }

    [Fact]
    public void SetMissingGenerateArchive()
    {
        using var tempDir = new TemporaryDirectory("0install-test-missing");
        string feedPath = Path.Combine(tempDir, "feed.xml");
        Directory.CreateDirectory(Path.Combine(tempDir, "impl"));
        FileUtils.Touch(Path.Combine(tempDir, "impl", "file"));

        var archive = new Archive {Href = new("archive.zip", UriKind.Relative)};
        var implementation = new Implementation {LocalPath = "impl", RetrievalMethods = {archive}};

        implementation.SetMissing(new SimpleCommandExecutor {Path = feedPath}, new SilentTaskHandler());

        implementation.LocalPath.Should().BeNull();
        implementation.ManifestDigest.Should().NotBe(default(ManifestDigest));
        archive.Size.Should().NotBe(0);

        File.Exists(Path.Combine(tempDir, "archive.zip")).Should().BeTrue();
    }

    [Fact]
    public void SetMissingDigestMismatch()
    {
        using var stream = typeof(ImplementationExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);
        var implementation = new Implementation {ManifestDigest = new ManifestDigest(sha1New: "invalid"), RetrievalMethods = {new Archive {Href = microServer.FileUri}}};
        Assert.Throws<DigestMismatchException>(() => implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler()));
    }
}
