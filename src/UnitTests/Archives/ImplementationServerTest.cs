// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Archives.Extractors;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives;

/// <summary>
/// Contains test methods for <see cref="ImplementationServer"/>.
/// </summary>
public class ImplementationServerTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir;
    private readonly ImplementationServer _server;
    private readonly HttpClient _client;

    public ImplementationServerTest()
    {
        Skip.If(WindowsUtils.IsWindowsNT && !WindowsUtils.IsAdministrator, "Listening on ports needs admin rights on Windows");

        _tempDir = new("0install-test-store");
        _server = new(new ImplementationStore(_tempDir, new SilentTaskHandler()));
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
        var digest = AddImplementation();
        using var response = await _client.SendAsync(new(HttpMethod.Head, $"{digest}.zip"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task HeadNotFound()
    {
        using var response = await _client.SendAsync(new(HttpMethod.Head, "sha256new_missing.zip"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetOK()
    {
        var digest = AddImplementation();
        using var stream = await _client.GetStreamAsync($"{digest}.zip");
        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        new ZipExtractor(new SilentTaskHandler()).Extract(builder, stream);
        ManifestFormat.Sha256New.DigestManifest(builder.Manifest).Should().Be(digest.Sha256New);
    }

    [SkippableFact]
    public async Task GetNotFound()
    {
        using var response = await _client.GetAsync("sha256new_missing.zip");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private ManifestDigest AddImplementation()
    {
        var digest = ManifestDigest.Empty;
        Directory.CreateDirectory(Path.Combine(_tempDir, digest.Best!));
        return digest;
    }
}
