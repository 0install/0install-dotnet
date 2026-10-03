// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using ZeroInstall.FileSystem;
using ZeroInstall.Services.Server;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Services.Fetchers;

/// <summary>
/// Contains test methods for <see cref="ImplementationDiscoveryTest"/>.
/// </summary>
[Collection(nameof(ImplementationServer))]
public class ImplementationDiscoveryTest : IDisposable
{
    static ImplementationDiscoveryTest()
    {
        ImplementationDiscovery.ExcludeLocalMachine = false;
    }

    private readonly TemporaryDirectory _tempDir;
    private readonly ImplementationStore _implementationStore;

    public ImplementationDiscoveryTest()
    {
        Assert.SkipWhen(WindowsUtils.IsWindowsNT && !WindowsUtils.IsAdministrator, "Listening on ports needs admin rights on Windows");
        _tempDir = new("0install-test-store");
        _implementationStore = new(_tempDir, new SilentTaskHandler());
    }

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    public void FoundServerStartedBefore()
    {
        SkipIfMulticastBlocked();
        var digest = AddImplementation();
        using var server = StartServer();

        using var discovery = new ImplementationDiscovery();
        var uri = discovery.TryGetImplementation(digest, _foundTimeout, TestContext.Current.CancellationToken);
        uri.Should().NotBeNull();
        uri!.LocalPath.Should().Contain(digest.Best);
    }

    [Fact]
    public async Task FoundServerStartedLater()
    {
        SkipIfMulticastBlocked();
        var digest = AddImplementation();
        using var discovery = new ImplementationDiscovery();

        // ReSharper disable once AccessToDisposedClosure
        var task = Task.Run(() => discovery.TryGetImplementation(digest, _foundTimeout, TestContext.Current.CancellationToken));
        using var server = StartServer();
        var uri = await task;
        uri.Should().NotBeNull();
        uri!.LocalPath.Should().Contain(digest.Best);
    }

    /// <summary>
    /// Upper bound for finding a server, so that broken discovery fails the test instead of hanging it.
    /// </summary>
    private static readonly TimeSpan _foundTimeout = TimeSpan.FromSeconds(10);

    private static void SkipIfMulticastBlocked()
        => Assert.SkipWhen(UnixUtils.IsMacOSX && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")),
            "macOS Local Network privacy blocks multicast for non-interactive processes like CI runners");

    [Fact]
    public void NotFound()
    {
        using var server = StartServer();
        using var discovery = new ImplementationDiscovery();
        discovery.TryGetImplementation(new(Sha256New: "dummy"), TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken)
                 .Should().BeNull();
    }

    [Fact]
    public void NoServer()
    {
        using var discovery = new ImplementationDiscovery();
        discovery.TryGetImplementation(new(Sha256New: "dummy"), TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken)
                 .Should().BeNull();
    }

    private ManifestDigest AddImplementation()
    {
        // Generate implementation with randomized contents/hash to avoid collisions with concurrent tests
        var testFile = new TestFile("file") {Contents = StringUtils.GeneratePassword(8)};
        var manifestBuilder = new ManifestBuilder(ManifestFormat.Sha256);
        manifestBuilder.AddFile(testFile.Name, testFile.Contents.ToStream(), testFile.LastWrite);
        var digest = new ManifestDigest(manifestBuilder.Manifest.CalculateDigest());

        _implementationStore.Add(digest, [testFile]);
        return digest;
    }

    private ImplementationServer StartServer()
        => new(_implementationStore);
}
