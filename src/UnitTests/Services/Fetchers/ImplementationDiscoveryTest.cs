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
        Skip.If(WindowsUtils.IsWindowsNT && !WindowsUtils.IsAdministrator, "Listening on ports needs admin rights on Windows");
        _tempDir = new("0install-test-store");
        _implementationStore = new(_tempDir, new SilentTaskHandler());
    }

    public void Dispose() => _tempDir.Dispose();

    [SkippableFact]
    public void FoundServerStartedBefore()
    {
        var digest = AddImplementation();
        using var server = StartServer();

        using var discovery = new ImplementationDiscovery();
        discovery.GetImplementation(digest, default)
                 .LocalPath.Should().Contain(digest.Best);
    }

    [SkippableFact]
    public async Task FoundServerStartedLater()
    {
        var digest = AddImplementation();
        using var discovery = new ImplementationDiscovery();

        // ReSharper disable once AccessToDisposedClosure
        var task = Task.Run(() => discovery.GetImplementation(digest, default));
        using var server = StartServer();
        (await task).LocalPath.Should().Contain(digest.Best);
    }

    [SkippableFact]
    public void NotFound()
    {
        using var server = StartServer();
        using var discovery = new ImplementationDiscovery();
        discovery.TryGetImplementation(new(Sha256New: "dummy"), TimeSpan.FromSeconds(1))
                 .Should().BeNull();
    }

    [SkippableFact]
    public void NoServer()
    {
        using var discovery = new ImplementationDiscovery();
        discovery.TryGetImplementation(new(Sha256New: "dummy"), TimeSpan.FromSeconds(1))
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
