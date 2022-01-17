// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using Moq;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Model;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Services.Fetchers;

/// <summary>
/// Runs test methods for <see cref="Fetcher"/>.
/// </summary>
public class FetcherTest : TestWithMocks
{
    private static readonly Stream _zipArchiveStream = typeof(FetcherTest).GetEmbeddedStream("testArchive.zip");
    private static readonly ManifestNormalFile _archiveRegularFile = new("regular\n".Hash(SHA256.Create()), new DateTime(2000, 1, 1, 11, 0, 0), 8);
    private static readonly ManifestExecutableFile _archiveExecutableFile = new("executable\n".Hash(SHA256.Create()), new DateTime(2000, 1, 1, 11, 0, 0), 11);
    private static readonly Manifest _archiveManifest = new(ManifestFormat.Sha256New)
    {
        [""] =
        {
            ["regular"] = _archiveRegularFile,
            ["executable"] = _archiveExecutableFile,
        }
    };

    private readonly MockTaskHandler _handler = new();
    private readonly Config _config = new();
    private readonly Mock<IImplementationStore> _storeMock;
    private readonly Fetcher _fetcher;

    public FetcherTest()
    {
        _storeMock = CreateMock<IImplementationStore>();
        _fetcher = new Fetcher(_config, _storeMock.Object, _handler);
    }

    [Fact]
    public void DownloadSingleArchive()
    {
        using var server = new MicroServer("archive.zip", _zipArchiveStream);
        TestDownload(_archiveManifest,
            new Archive
            {
                Href = server.FileUri,
                MimeType = Archive.MimeTypeZip,
                Size = _zipArchiveStream.Length
            });
    }

    [Fact]
    public void DownloadSingleArchiveMirror()
    {
        using var mirrorServer = new MicroServer("archive/http/invalid/directory%23archive.zip", _zipArchiveStream);
        _config.FeedMirror = new(mirrorServer.ServerUri);
        TestDownload(_archiveManifest,
            new Archive
            {
                Href = new("http://invalid/directory/archive.zip"),
                MimeType = Archive.MimeTypeZip,
                Size = _zipArchiveStream.Length
            });
    }

    [Fact]
    public void DownloadLocalArchive()
    {
        using var tempFile = new TemporaryFile("0install-test-archive");
        _zipArchiveStream.CopyToFile(tempFile);
        TestDownload(
            _archiveManifest,
            new Archive
            {
                Href = new(tempFile),
                MimeType = Archive.MimeTypeZip,
                Size = _zipArchiveStream.Length
            });
    }

    [Fact]
    public void DownloadMultipleArchives()
    {
        using var server1 = new MicroServer("archive.zip", _zipArchiveStream);
        using var server2 = new MicroServer("archive.zip", _zipArchiveStream);
        TestDownload(new(ManifestFormat.Sha256New)
            {
                ["destination1"] =
                {
                    ["regular"] = _archiveRegularFile,
                    ["executable"] = _archiveExecutableFile,
                },
                ["destination2"] =
                {
                    ["regular"] = _archiveRegularFile,
                    ["executable"] = _archiveExecutableFile,
                }
            },
            new Recipe
            {
                Steps =
                {
                    new Archive {Href = server1.FileUri, MimeType = Archive.MimeTypeZip, Size = _zipArchiveStream.Length, Destination = "destination1"},
                    new Archive {Href = server2.FileUri, MimeType = Archive.MimeTypeZip, Size = _zipArchiveStream.Length, Destination = "destination2"}
                }
            });
    }

    [Fact]
    public void DownloadSingleFile()
    {
        using var server = new MicroServer("regular", TestFile.DefaultContents.ToStream());
        TestDownload(
            new(ManifestFormat.Sha256)
            {
                [""] =
                {
                    ["regular"] = new ManifestNormalFile(TestFile.DefaultContents.Hash(SHA256.Create()), 0, TestFile.DefaultContents.Length)
                }
            },
            new SingleFile {Href = server.FileUri, Size = TestFile.DefaultContents.Length, Destination = "regular"});
    }

    [Fact]
    public void DownloadRecipe()
    {
        using var serverArchive = new MicroServer("archive.zip", _zipArchiveStream);
        using var serverSingleFile = new MicroServer("regular", TestFile.DefaultContents.ToStream());
        TestDownload(
            new(ManifestFormat.Sha256)
            {
                [""] =
                {
                    ["regular"] = _archiveRegularFile,
                    ["regular2"] = new ManifestNormalFile(TestFile.DefaultContents.Hash(SHA256.Create()), 0, TestFile.DefaultContents.Length),
                    ["executable2"] = _archiveExecutableFile,
                }
            },
            new Recipe
            {
                Steps =
                {
                    new Archive {Href = serverArchive.FileUri, MimeType = Archive.MimeTypeZip, Size = _zipArchiveStream.Length},
                    new RenameStep {Source = "executable", Destination = "executable2"},
                    new SingleFile {Href = serverSingleFile.FileUri, Size = TestFile.DefaultContents.Length, Destination = "regular2"}
                }
            });
    }

    [Fact]
    public void SkipBroken()
    {
        using var serverArchive = new MicroServer("archive.zip", _zipArchiveStream);
        using var serverSingleFile = new MicroServer("regular", TestFile.DefaultContents.ToStream());
        TestDownload(
            new(ManifestFormat.Sha256)
            {
                [""] =
                {
                    ["regular"] = new ManifestNormalFile(TestFile.DefaultContents.Hash(SHA256.Create()), 0, TestFile.DefaultContents.Length)
                }
            },
            // broken: wrong size
            new Archive {Href = serverArchive.FileUri, MimeType = Archive.MimeTypeZip, Size = 0},
            // broken: unknown archive format
            new Archive {Href = serverArchive.FileUri, MimeType = "test/format", Size = _zipArchiveStream.Length},
            // works
            new Recipe {Steps = {new SingleFile {Href = serverSingleFile.FileUri, Size = TestFile.DefaultContents.Length, Destination = "regular"}}});
    }

    private void TestDownload(Manifest expected, params RetrievalMethod[] retrievalMethod)
    {
        var digest = new ManifestDigest(expected.CalculateDigest());
        var testImplementation = new Implementation {ID = "test", ManifestDigest = digest};
        testImplementation.RetrievalMethods.AddRange(retrievalMethod);

        _storeMock.Setup(x => x.Add(It.IsAny<ManifestDigest>(), It.IsAny<Action<IBuilder>>()))
                  .Callback((ManifestDigest manifestDigest, Action<IBuilder> build) =>
                   {
                       var builder = new ManifestBuilder(ManifestFormat.FromPrefix(manifestDigest.Best!));
                       build(builder);
                       builder.Manifest.Should().BeEquivalentTo(expected);
                   });

        _storeMock.Setup(x => x.GetPath(digest))
                  .Returns(() => null);

        _fetcher.Fetch(testImplementation);
    }

    [Fact]
    public void SkipExisting()
    {
        var digest = new ManifestDigest(sha256New: "test123");
        var testImplementation = new Implementation {ID = "test", ManifestDigest = digest, RetrievalMethods = {new Recipe()}};
        _storeMock.Setup(x => x.GetPath(digest)).Returns("dummy");

        _fetcher.Fetch(testImplementation);
    }

    [Fact]
    public void NoSuitableMethod()
    {
        var implementation = new Implementation {ID = "test", ManifestDigest = new(sha256New: "test123")};
        _storeMock.Setup(x => x.GetPath(implementation.ManifestDigest)).Returns(() => null);

        Assert.Throws<NotSupportedException>(() => _fetcher.Fetch(implementation));
    }

    [Fact]
    public void UnsupportedArchiveFormat()
    {
        var implementation = new Implementation
        {
            ID = "test",
            ManifestDigest = new(sha256New: "test123"),
            RetrievalMethods = {new Archive {MimeType = "test/format"}}
        };
        _storeMock.Setup(x => x.GetPath(implementation.ManifestDigest)).Returns(() => null);

        Assert.Throws<NotSupportedException>(() => _fetcher.Fetch(implementation));
    }

    [Fact]
    public void UnsupportedArchiveFormatInRecipe()
    {
        var implementation = new Implementation
        {
            ID = "test",
            ManifestDigest = new(sha256New: "test123"),
            RetrievalMethods = {new Recipe {Steps = {new Archive {MimeType = Archive.MimeTypeZip}, new Archive {MimeType = "test/format"}}}}
        };
        _storeMock.Setup(x => x.GetPath(implementation.ManifestDigest)).Returns(() => null);

        Assert.Throws<NotSupportedException>(() => _fetcher.Fetch(implementation));
    }

    [Fact]
    public void RunExternalConfirm()
    {
        bool installInvoked = false;
        _handler.AnswerQuestionWith = true;
        _fetcher.Fetch(new Implementation
        {
            ID = ExternalImplementation.PackagePrefix + "123",
            RetrievalMethods =
            {
                new ExternalRetrievalMethod
                {
                    ConfirmationQuestion = "Install?",
                    Install = () => { installInvoked = true; }
                }
            }
        });
        installInvoked.Should().BeTrue();
    }

    [Fact]
    public void RunExternalDeny()
    {
        bool installInvoked = false;
        _handler.AnswerQuestionWith = false;
        Assert.Throws<OperationCanceledException>(() => _fetcher.Fetch(new Implementation
        {
            ID = ExternalImplementation.PackagePrefix + "123",
            RetrievalMethods =
            {
                new ExternalRetrievalMethod
                {
                    ConfirmationQuestion = "Install?",
                    Install = () => { installInvoked = true; }
                }
            }
        }));
        installInvoked.Should().BeFalse();
    }
}
