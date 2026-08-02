// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Net.Sockets;
using System.Text;
using NanoByte.Common.Native;
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

    /// <summary>
    /// Large enough that streaming it takes many round-trips, ensuring the client has read the start of the
    /// response before the failure ends the connection. Filled with non-zero data so that
    /// <see cref="EndOfArchiveMarker"/> can be detected.
    /// </summary>
    private const int BigFileSize = 4 * 1024 * 1024;

    /// <summary>TAR archives end with two zero-filled blocks, written when the archive builder is disposed.</summary>
    private const int EndOfArchiveMarker = 2 * 512;

    /// <summary>Marks the end of a chunked HTTP response body.</summary>
    private const string TerminatingChunk = "0\r\n\r\n";

    [Fact]
    public void GetTruncatedOnMidArchiveFailure()
    {
        var digest = RandomDigest();
        string implDir = Path.Combine(_tempDir, digest.Best!);

        // Files in the root are read before files in subdirectories, so the big file is streamed first
        Directory.CreateDirectory(implDir);
        File.WriteAllBytes(Path.Combine(implDir, "big.dat"), Enumerable.Repeat((byte)0xAB, BigFileSize).ToArray());

        // Files rejected by the manifest are skipped without being registered with the archive builder, so a
        // hardlink to one makes the builder fail once it is reached, i.e. after streaming has already started
        string skipped = Path.Combine(implDir, ".xbit");
        File.WriteAllBytes(skipped, [1, 2, 3]);
        Directory.CreateDirectory(Path.Combine(implDir, "sub"));
        FileUtils.CreateHardlink(Path.Combine(implDir, "sub", "link.dat"), skipped);

        byte[] response = RawRequest($"GET /{digest}.tar HTTP/1.1\r\nHost: localhost:{_server.Port}\r\nConnection: close\r\n\r\n");
        string text = Encoding.ASCII.GetString(response);

        // Since the response has no ContentLength64, the headers are only sent along with the first part of the
        // body. Seeing them proves that the archive was already being streamed when the failure happened.
        // How much of the body arrives is not deterministic, because aborting discards data that is still buffered.
        text.Should().StartWith("HTTP/1.1 200", "the headers are already on the wire when the failure happens");
        text.Should().Contain("Transfer-Encoding: chunked");

        // Disposing the archive builder would write the end-of-archive marker, turning the partial archive into
        // a well-formed one. Its absence proves that the builder was abandoned instead.
        ZeroRun(response).Should().BeLessThan(EndOfArchiveMarker, "an incomplete archive must not be finalized");

        // Response.Close() would write the terminating chunk, presenting the partial archive as a complete
        // transfer. Its absence is what proves that HttpServer aborted the response instead.
        // Only http.sys actually truncates here; HttpListenerResponse.Abort() on other platforms still
        // terminates the chunked response cleanly, so the client cannot tell the transfer was cut short.
        if (WindowsUtils.IsWindows)
            text.Should().NotEndWith(TerminatingChunk, "a corrupt archive must not be presented as a complete transfer");
    }

    /// <summary>
    /// Returns the length of the longest run of zero bytes in <paramref name="data"/>.
    /// </summary>
    private static int ZeroRun(byte[] data)
    {
        int longest = 0, current = 0;
        foreach (byte b in data)
        {
            current = b == 0 ? current + 1 : 0;
            if (current > longest) longest = current;
        }
        return longest;
    }

    /// <summary>
    /// Sends a raw HTTP request and reads the response until the server closes or resets the connection.
    /// </summary>
    private byte[] RawRequest(string request)
    {
        // A small receive buffer keeps the server blocked on the big file rather than letting it buffer the
        // entire response, so that the client reliably reads the data that was sent before the failure
        using var client = new TcpClient {ReceiveBufferSize = 8192, ReceiveTimeout = 30000};

        // Connect via "localhost" hostname resolution (tries all addresses) to match whichever address the listener is bound to
        client.Connect("localhost", _server.Port);
        using var stream = client.GetStream();

        byte[] requestBytes = Encoding.ASCII.GetBytes(request);
        stream.Write(requestBytes, 0, requestBytes.Length);

        var response = new MemoryStream();
        byte[] buffer = new byte[8192];
        try
        {
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                response.Write(buffer, 0, read);
        }
        catch (IOException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionReset })
        {
            // Aborted by the server, which may reset rather than close the connection
        }

        return response.ToArray();
    }

    private static ManifestDigest RandomDigest() => new(Sha256New: StringUtils.GeneratePassword(8));
}
