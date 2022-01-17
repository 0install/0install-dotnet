// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Model;
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
        _store = new IconStore(_tempDir, new Config(), new SilentTaskHandler());
    }

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    public void ShouldEnsureCorrectFileExtension()
    {
        string path = _store.GetPath(PngIcon(new("http://host/file")));
        Path.GetExtension(path).Should().Be(".png");
    }

    [Fact]
    public void ShouldReturnCached()
    {
        var icon = PngIcon(new("http://example.com/test1.png"));
        Inject(icon, iconData: "icon");
        VerifyGet(icon, iconData: "icon");
    }

    [Fact]
    public void ShouldDownloadMissing()
    {
        using var server = new MicroServer("icon.png", "data".ToStream());
        var icon = PngIcon(server.FileUri);
        VerifyGet(icon, "data");
    }

    [SkippableFact]
    public void ShouldRefreshStale()
    {
        Skip.IfNot(NetUtils.IsInternetConnected, "Icon cache is not refresh when offline");

        using var server = new MicroServer("icon.png", "new".ToStream());
        var icon = PngIcon(server.FileUri);
        Inject(icon, "old", timestamp: new DateTime(1980, 1, 1));
        VerifyGet(icon, "new");
    }

    [Fact]
    public void ShouldReturnStaleOnRefreshFailure()
    {
        using var server = new MicroServer("_", new MemoryStream());
        var icon = PngIcon(new(server.FileUri + "-invalid"));
        Inject(icon, "data", timestamp: new DateTime(1980, 1, 1));
        VerifyGet(icon, "data");
    }

    private static Icon PngIcon(Uri href) => new() {Href = href, MimeType = Icon.MimeTypePng};

    private void Inject(Icon icon, string iconData, DateTime? timestamp = null)
    {
        string path = _store.GetPath(icon);
        File.WriteAllText(path, iconData);
        if (timestamp.HasValue) File.SetLastWriteTimeUtc(path, timestamp.Value);
    }

    private void VerifyGet(Icon icon, string iconData)
        => File.ReadAllText(_store.Get(icon)).Should().Be(iconData);
}
