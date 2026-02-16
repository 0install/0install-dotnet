// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Archives.Extractors;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Server;

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
        _tempDir = new("0install-test-store");
        _implementationStore = new ImplementationStore(_tempDir, new SilentTaskHandler());
        _server = new(_implementationStore, localOnly: true);
        _client = new() {BaseAddress = new($"http://localhost:{_server.Port}/")};
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
        _tempDir.Dispose();
    }

    [Fact]
    public async Task HeadOK()
    {
        var digest = RandomDigest();
        _implementationStore.Add(digest, [new TestFile("fileA")]);

        using var response = await _client.SendAsync(new(HttpMethod.Head, $"{digest}.zip"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HeadNotFound()
    {
        using var response = await _client.SendAsync(new(HttpMethod.Head, "sha256new_dummy.zip"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOK()
    {
        var digest = RandomDigest();
        _implementationStore.Add(digest, [new TestFile("fileA")]);

        using var stream = await _client.GetStreamAsync($"{digest}.zip"
#if NET
            , TestContext.Current.CancellationToken
#endif
        );
        new ZipExtractor(new SilentTaskHandler()).Extract(Mock.Of<IBuilder>(), stream);
    }

    [Fact]
    public async Task GetNotFound()
    {
        using var response = await _client.GetAsync("sha256new_dummy.zip", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static ManifestDigest RandomDigest() => new(Sha256New: StringUtils.GeneratePassword(8));
}
