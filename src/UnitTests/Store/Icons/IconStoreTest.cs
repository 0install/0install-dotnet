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
    private readonly Config _config = new();
    private readonly IconStore _store;

    public IconStoreTest()
    {
        _store = new(_tempDir, _config, new SilentTaskHandler());
    }

    public void Dispose() => _tempDir.Dispose();

    [Theory, InlineData(Icon.MimeTypePng, ".png"), InlineData(Icon.MimeTypeIco, ".ico")]
    public void EnsureCorrectFileExtensionPng(string mimeType, string extension)
    {
        string name = IconStore.GetFileName(new() {Href = new("http://example.com/file"), MimeType = mimeType});
        Path.GetExtension(name).Should().Be(extension);
    }

    [Fact]
    public void DownloadMissingPng() => DownloadMissing(_pngBytes, Icon.MimeTypePng);

    [Fact]
    public void DownloadMissingIco() => DownloadMissing(_icoBytes, Icon.MimeTypeIco);

    private void DownloadMissing(byte[] bytes, string mimeType)
    {
        using var server = new MicroServer("file", bytes.ToStream());
        File.ReadAllBytes(_store.GetFresh(new Icon {Href = server.FileUri, MimeType = mimeType}))
            .Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public void DontDownloadInOfflineMode()
    {
        _config.NetworkUse = NetworkLevel.Offline;
        using var server = new MicroServer("file", _dummyBytes.ToStream());
        var icon = new Icon {Href = server.FileUri, MimeType = Icon.MimeTypePng};
        _store.Invoking(x => x.GetFresh(icon))
              .Should().Throw<WebException>();
    }

    [Theory, InlineData(Icon.MimeTypePng), InlineData(Icon.MimeTypeIco)]
    public void RejectDamagedDownload(string mimeType)
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Icon validation currently uses GDI+ which is only available on Windows");

        using var server = new MicroServer("file", _dummyBytes.ToStream());
        var icon = new Icon {Href = server.FileUri, MimeType = mimeType};
        _store.Invoking(x => x.Get(icon, out _))
              .Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ReturnCachedWhenFresh()
    {
        Inject(_dummyPngIcon, _pngBytes);
        File.ReadAllBytes(_store.GetFresh(_dummyPngIcon))
            .Should().BeEquivalentTo(_pngBytes);
    }

    [Fact]
    public void DontSuggestRefreshWhenFresh()
    {
        Inject(_dummyPngIcon, _pngBytes);
        _store.Get(_dummyPngIcon, out bool shouldRefresh);
        shouldRefresh.Should().BeFalse();
    }

    [Fact]
    public void SuggestRefreshWhenStale()
    {
        Assert.SkipUnless(NetUtils.GetInternetConnectivity() == Connectivity.Normal, "Requires normal internet connectivity");

        Inject(_dummyPngIcon, _pngBytes, _oldTimestamp);
        _store.Get(_dummyPngIcon, out bool shouldRefresh);
        shouldRefresh.Should().BeTrue();
    }

    [Fact]
    public void DontSuggestRefreshInOfflineMode()
    {
        _config.NetworkUse = NetworkLevel.Offline;
        Inject(_dummyPngIcon, _pngBytes, _oldTimestamp);
        _store.Get(_dummyPngIcon, out bool shouldRefresh);
        shouldRefresh.Should().BeFalse();
    }

    [Fact]
    public void RefreshWhenStale()
    {
        Assert.SkipUnless(NetUtils.GetInternetConnectivity() == Connectivity.Normal, "Requires normal internet connectivity");

        using var server = new MicroServer("icon.png", _pngBytes.ToStream());
        var icon = new Icon {Href = server.FileUri, MimeType = Icon.MimeTypePng};
        Inject(icon, _dummyBytes, _oldTimestamp);
        File.ReadAllBytes(_store.GetFresh(icon))
            .Should().BeEquivalentTo(_pngBytes);
    }

    [Fact]
    public void ReturnStaleOnRefreshFailure()
    {
        using var server = new MicroServer("_", new MemoryStream());
        var icon = new Icon {Href = new($"{server.FileUri}-invalid"), MimeType = Icon.MimeTypePng};
        Inject(icon, _pngBytes, _oldTimestamp);
        File.ReadAllBytes(_store.GetFresh(icon))
            .Should().BeEquivalentTo(_pngBytes);
    }


    [Fact]
    public void ImportPng() => Import(_pngBytes, Icon.MimeTypePng);

    [Fact]
    public void ImportIco() => Import(_icoBytes, Icon.MimeTypeIco);

    private void Import(byte[] bytes, string mimeType)
    {
        var icon = new Icon {Href = new("https://example.com/file"), MimeType = mimeType};
        _store.Import(icon, bytes.ToStream());
        File.ReadAllBytes(_store.TryGetCached(icon)!)
            .Should().BeEquivalentTo(bytes);
    }

    [Theory, InlineData(Icon.MimeTypePng), InlineData(Icon.MimeTypeIco)]
    public void RejectDamagedImport(string mimeType)
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Icon validation currently uses GDI+ which is only available on Windows");

        var icon = new Icon {Href = new("https://example.com/file"), MimeType = mimeType};
        _store.Invoking(x => x.Import(icon, _dummyBytes.ToStream()))
              .Should().Throw<InvalidDataException>();
    }

    private static readonly byte[]
        _dummyBytes = [1, 2, 3],
        _pngBytes = typeof(IconStoreTest).GetEmbeddedBytes("icon.png"),
        _icoBytes = typeof(IconStoreTest).GetEmbeddedBytes("icon.ico");

    private static readonly Icon _dummyPngIcon = new() {Href = new("http://example.com/test1.png"), MimeType = Icon.MimeTypePng};
    private static readonly DateTime _oldTimestamp = new(1980, 1, 1);

    private void Inject(Icon icon, byte[] iconBytes, DateTime? timestamp = null)
    {
        string path = Path.Combine(_tempDir, IconStore.GetFileName(icon));
        File.WriteAllBytes(path, iconBytes);
        if (timestamp.HasValue) File.SetLastWriteTimeUtc(path, timestamp.Value);
    }
}
