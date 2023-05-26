// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Archives;
using ZeroInstall.Store.Implementations;

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

    public ImplementationDiscoveryTest()
    {
        Skip.If(WindowsUtils.IsWindowsNT && !WindowsUtils.IsAdministrator, "Listening on ports needs admin rights on Windows");
        _tempDir = new("0install-test-store");
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
        discovery.TryGetImplementation(ManifestDigest.Empty, TimeSpan.FromSeconds(1))
                 .Should().BeNull();
    }

    [SkippableFact]
    public void NoServer()
    {
        using var discovery = new ImplementationDiscovery();
        discovery.TryGetImplementation(ManifestDigest.Empty, TimeSpan.FromSeconds(1))
                 .Should().BeNull();
    }

    private ManifestDigest AddImplementation()
    {
        var digest = ManifestDigest.Empty;
        Directory.CreateDirectory(Path.Combine(_tempDir, digest.Best!));
        return digest;
    }

    private ImplementationServer StartServer()
        => new(new ImplementationStore(_tempDir, new SilentTaskHandler()));
}
