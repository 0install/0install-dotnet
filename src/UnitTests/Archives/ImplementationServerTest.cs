// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Archives.Extractors;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Archives;

/// <summary>
/// Contains test methods for <see cref="ImplementationServer"/>.
/// </summary>
[Collection(nameof(ImplementationServer))]
public class ImplementationServerTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir;
    private readonly ImplementationStore _implementationStore;
    private readonly ImplementationServer _server;
    private readonly HttpClient _client;

    public ImplementationServerTest()
    {
        Skip.If(WindowsUtils.IsWindowsNT && !WindowsUtils.IsAdministrator, "Listening on ports needs admin rights on Windows");

        _tempDir = new("0install-test-store");
        _implementationStore = new ImplementationStore(_tempDir, new SilentTaskHandler());
        _server = new(_implementationStore);
        _client = new() {BaseAddress = new($"http://localhost:{_server.Port}/")};
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
        _tempDir.Dispose();
    }

    [SkippableFact]
    public async Task HeadOK()
    {
        var digest = RandomDigest();
        ImplementationStoreExtensions.Add(_implementationStore, digest, new() {new TestFile("fileA")});

        using var response = await _client.SendAsync(new(HttpMethod.Head, $"{digest}.zip"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task HeadNotFound()
    {
        using var response = await _client.SendAsync(new(HttpMethod.Head, "sha256new_dummy.zip"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetOK()
    {
        var digest = RandomDigest();
        ImplementationStoreExtensions.Add(_implementationStore, digest, new() {new TestFile("fileA")});

        using var stream = await _client.GetStreamAsync($"{digest}.zip");
        new ZipExtractor(new SilentTaskHandler()).Extract(Mock.Of<IBuilder>(), stream);
    }

    [SkippableFact]
    public async Task GetNotFound()
    {
        using var response = await _client.GetAsync("sha256new_dummy.zip");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static ManifestDigest RandomDigest() => new(Sha256New: StringUtils.GeneratePassword(8));
}
