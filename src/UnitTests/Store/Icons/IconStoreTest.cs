// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Store.Icons;

/// <summary>
/// Contains test methods for <see cref="IconStore"/>.
/// </summary>
public class IconStoreTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir = new("0install-test-icons");
    private readonly IconStore _store;

    public IconStoreTest()
    {
        _store = new(_tempDir, new Config(), new SilentTaskHandler());
    }

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    public void ShouldEnsureCorrectFileExtensionPng()
    {
        string name = IconStore.GetFileName(new() {Href = new("http://example.com/file"), MimeType = Icon.MimeTypePng});
        Path.GetExtension(name).Should().Be(".png");
    }

    [Fact]
    public void ShouldEnsureCorrectFileExtensionIco()
    {
        string name = IconStore.GetFileName(new() {Href = new("http://example.com/file"), MimeType = Icon.MimeTypeIco});
        Path.GetExtension(name).Should().Be(".ico");
    }

    [Fact]
    public void ShouldReturnCached()
    {
        var iconBytes = typeof(IconStoreTest).GetEmbeddedBytes("icon.png");
        var icon = new Icon {Href = new("http://example.com/test1.png"), MimeType = Icon.MimeTypePng};
        Inject(icon, iconBytes);

        File.ReadAllBytes(_store.GetFresh(icon))
            .Should().BeEquivalentTo(iconBytes);
    }

    [Fact]
    public void ShouldDownloadMissingPng()
    {
        using var iconStream = typeof(IconStoreTest).GetEmbeddedStream("icon.png");
        using var server = new MicroServer("icon.png", iconStream);
        var icon = new Icon {Href = server.FileUri, MimeType = Icon.MimeTypePng};

        File.ReadAllBytes(_store.GetFresh(icon))
            .Should().BeEquivalentTo(iconStream.AsArray());
    }

    [Fact]
    public void ShouldDownloadMissingIco()
    {
        using var iconStream = typeof(IconStoreTest).GetEmbeddedStream("icon.ico");
        using var server = new MicroServer("icon.ico", iconStream);
        var icon = new Icon {Href = server.FileUri, MimeType = Icon.MimeTypeIco};

        File.ReadAllBytes(_store.GetFresh(icon))
            .Should().BeEquivalentTo(iconStream.AsArray());
    }

    [SkippableFact]
    public void ShouldRejectDamagedPng()
    {
        Skip.IfNot(WindowsUtils.IsWindows, "Icon validation currently uses GDI+ which is only available on Windows");

        using var server = new MicroServer("icon.png", new byte[] {1, 2, 3}.ToStream());
        var icon = new Icon {Href = server.FileUri, MimeType = Icon.MimeTypePng};

        _store.Invoking(x => x.Get(icon, out _))
              .Should().Throw<InvalidDataException>();
    }

    [SkippableFact]
    public void ShouldRejectDamagedIco()
    {
        Skip.IfNot(WindowsUtils.IsWindows, "Icon validation currently uses GDI+ which is only available on Windows");

        using var server = new MicroServer("icon.ico", new byte[] {1, 2, 3}.ToStream());
        var icon = new Icon {Href = server.FileUri, MimeType = Icon.MimeTypeIco};

        _store.Invoking(x => x.Get(icon, out _))
              .Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ShouldRefreshStale()
    {
        using var iconStream = typeof(IconStoreTest).GetEmbeddedStream("icon.png");
        using var server = new MicroServer("icon.png", iconStream);
        var icon = new Icon {Href = server.FileUri, MimeType = Icon.MimeTypePng};
        Inject(icon, new byte[] {1, 2, 3}, timestamp: new DateTime(1980, 1, 1));

        File.ReadAllBytes(_store.GetFresh(icon))
            .Should().BeEquivalentTo(iconStream.AsArray());
    }

    [Fact]
    public void ShouldReturnStaleOnRefreshFailure()
    {
        using var server = new MicroServer("_", new MemoryStream());
        var icon = new Icon {Href = new(server.FileUri + "-invalid"), MimeType = Icon.MimeTypePng};

        var iconBytes = typeof(IconStoreTest).GetEmbeddedBytes("icon.png");
        Inject(icon, iconBytes, timestamp: new DateTime(1980, 1, 1));

        File.ReadAllBytes(_store.GetFresh(icon))
            .Should().BeEquivalentTo(iconBytes);
    }

    private void Inject(Icon icon, byte[] iconBytes, DateTime? timestamp = null)
    {
        string path = Path.Combine(_tempDir, IconStore.GetFileName(icon));
        File.WriteAllBytes(path, iconBytes);
        if (timestamp.HasValue) File.SetLastWriteTimeUtc(path, timestamp.Value);
    }
}
