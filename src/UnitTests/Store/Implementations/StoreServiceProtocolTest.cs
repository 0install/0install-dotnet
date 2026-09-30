// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.Principal;
using NanoByte.Common.Native;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Sends hand-crafted (partially malformed) messages to <see cref="StoreServiceServer"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public class StoreServiceProtocolTest : IDisposable
{
    private const string ValidDigest = "sha256new_4OYMIQUY7QOBJGX36TEJS35ZEQT24QPEMSNZGTFESWMRW6CSXBKQ"; // Empty directory

    private readonly TemporaryDirectory _storeDir = StoreServiceTest.CreateSecureDirectory();
    private readonly string _pipeName = "ZeroInstall.Store.Service.Test." + Guid.NewGuid();
    private readonly StoreServiceServer _server;

    public StoreServiceProtocolTest()
    {
        Assert.SkipUnless(WindowsUtils.IsWindowsNT, "Store Service is only available on Windows");
        _server = new StoreServiceServer([_storeDir], _pipeName, new());
        _server.Start();
    }

    public void Dispose()
    {
        _server?.Dispose();
        _storeDir.Dispose();
    }

    private sealed class RawClient : IDisposable
    {
        private readonly NamedPipeClientStream _pipe;
        public StoreServiceWriter Writer { get; }
        public StoreServiceReader Reader { get; }

        public RawClient(string pipeName)
        {
            _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            _pipe.Connect(5000);
            Writer = new StoreServiceWriter(_pipe);
            Reader = new StoreServiceReader(_pipe);
        }

        public void Hello(params string[] digests)
        {
            Writer.Write(StoreServiceProtocol.Magic, 0, StoreServiceProtocol.Magic.Length);
            Writer.WriteUInt16(StoreServiceProtocol.Version);
            Writer.WriteByte((byte)digests.Length);
            foreach (string digest in digests)
                Writer.WriteString(digest, 1000);
        }

        public void Op(StoreServiceOp op, params string[] strings)
        {
            Writer.WriteByte((byte)op);
            foreach (string value in strings)
                Writer.WriteString(value, 10000);
        }

        public StoreServiceResponse Response() => Reader.ReadResponse();

        public void Dispose() => _pipe.Dispose();
    }

    private StoreServiceResponse Send(Action<RawClient> action)
    {
        using var client = new RawClient(_pipeName);
        try
        {
            action(client);
        }
        catch (IOException)
        {
            // Server may close the connection before we are done sending
        }
        return client.Response();
    }

    [Fact]
    public void AcceptsValidRequest()
    {
        using var client = new RawClient(_pipeName);
        client.Hello(ValidDigest);
        client.Response().Result.Should().Be(StoreServiceResult.Ok);
        client.Op(StoreServiceOp.Commit);
        client.Response().Result.Should().Be(StoreServiceResult.Ok);

        Directory.Exists(Path.Combine(_storeDir, ValidDigest)).Should().BeTrue();
    }

    [Fact]
    public void RejectsWrongMagic()
    {
        Send(client => client.Writer.Write([1, 2, 3, 4, 5, 6, 7, 8], 0, 8))
           .Result.Should().Be(StoreServiceResult.InvalidRequest);
        AcceptsValidRequest(); // Server still healthy
    }

    [Fact]
    public void RejectsUnknownVersion()
    {
        Send(client =>
        {
            client.Writer.Write(StoreServiceProtocol.Magic, 0, StoreServiceProtocol.Magic.Length);
            client.Writer.WriteUInt16(99);
        }).Result.Should().Be(StoreServiceResult.NotSupported);
    }

    [Theory]
    [InlineData("sha1new=da39a3ee5e6b4b0d3255bfef95601890afd80709", (byte)StoreServiceResult.NotSupported)]
    [InlineData("sha256new_../../../Windows", (byte)StoreServiceResult.InvalidRequest)]
    [InlineData("sha256=E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", (byte)StoreServiceResult.InvalidRequest)]
    [InlineData("md5=d41d8cd98f00b204e9800998ecf8427e", (byte)StoreServiceResult.InvalidRequest)]
    public void RejectsBadDigests(string digest, byte expected)
        => Send(client => client.Hello(digest)).Result.Should().Be((StoreServiceResult)expected);

    [Fact]
    public void RejectsTooManyDigests()
        => Send(client => client.Hello(ValidDigest, ValidDigest, ValidDigest, ValidDigest, ValidDigest))
          .Result.Should().Be(StoreServiceResult.InvalidRequest);

    [Fact]
    public void RejectsOverlongStrings()
        => Send(client => client.Hello(new string('a', 1000)))
          .Result.Should().Be(StoreServiceResult.LimitExceeded);

    [Fact]
    public void RejectsInvalidUtf8()
        => Send(client =>
        {
            client.Writer.Write(StoreServiceProtocol.Magic, 0, StoreServiceProtocol.Magic.Length);
            client.Writer.WriteUInt16(StoreServiceProtocol.Version);
            client.Writer.WriteByte(1);
            client.Writer.WriteUInt16(2);
            client.Writer.Write([0xC3, 0x28], 0, 2);
        }).Result.Should().Be(StoreServiceResult.InvalidRequest);

    [Theory]
    [InlineData("..")]
    [InlineData("a/../../b")]
    [InlineData("a\\..\\..\\b")]
    [InlineData("/absolute")]
    [InlineData("C:/Windows")]
    [InlineData("a//b")]
    [InlineData("file:stream")]
    [InlineData("COM1")]
    [InlineData("nul.txt")]
    [InlineData("trailing ")]
    [InlineData("new\nline")]
    public void RejectsInvalidPaths(string path)
    {
        Send(client =>
        {
            client.Hello(ValidDigest);
            client.Response().Result.Should().Be(StoreServiceResult.Ok);
            client.Op(StoreServiceOp.AddDirectory, path);
            client.Op(StoreServiceOp.Commit);
        }).Result.Should().Be(StoreServiceResult.InvalidRequest);
    }

    [Fact]
    public void RejectsUnknownOperation()
        => Send(client =>
        {
            client.Hello(ValidDigest);
            client.Response();
            client.Writer.WriteByte(0x42);
        }).Result.Should().Be(StoreServiceResult.InvalidRequest);

    [Fact]
    public void RejectsOversizedChunk()
        => Send(client =>
        {
            client.Hello(ValidDigest);
            client.Response();
            client.Op(StoreServiceOp.AddFile, "file");
            client.Writer.WriteInt64(0);
            client.Writer.WriteBool(false);
            client.Writer.WriteInt64(-1);
            client.Writer.WriteUInt32(StoreServiceProtocol.MaxChunkSize + 1);
        }).Result.Should().Be(StoreServiceResult.InvalidRequest);

    [Fact]
    public void RejectsLengthMismatch()
        => Send(client =>
        {
            client.Hello(ValidDigest);
            client.Response();
            client.Op(StoreServiceOp.AddFile, "file");
            client.Writer.WriteInt64(0);
            client.Writer.WriteBool(false);
            client.Writer.WriteInt64(10);
            client.Writer.WriteUInt32(20);
            client.Writer.Write(new byte[20], 0, 20);
        }).Result.Should().Be(StoreServiceResult.InvalidRequest);

    [Fact]
    public void RejectsInvalidModifiedTime()
        => Send(client =>
        {
            client.Hello(ValidDigest);
            client.Response();
            client.Op(StoreServiceOp.AddFile, "file");
            client.Writer.WriteInt64(long.MaxValue);
        }).Result.Should().Be(StoreServiceResult.InvalidRequest);

    [Fact]
    public void EnforcesTotalPathLengthLimit()
    {
        using var storeDir = StoreServiceTest.CreateSecureDirectory();
        string pipeName = _pipeName + ".PathLength";
        using var server = new StoreServiceServer([storeDir], pipeName, new() {MaxTotalPathLength = 100});
        server.Start();

        using var client = new RawClient(pipeName);
        client.Hello(ValidDigest);
        client.Response().Result.Should().Be(StoreServiceResult.Ok);
        try
        {
            for (int i = 0; i < 10; i++)
                client.Op(StoreServiceOp.AddDirectory, $"dir{i}-" + new string('a', 20));
            client.Op(StoreServiceOp.Commit);
        }
        catch (IOException)
        {
            // Server may close the connection before we are done sending
        }

        var response = client.Response();
        response.Result.Should().Be(StoreServiceResult.LimitExceeded);
        response.Message.Should().Contain("path length");
    }

    [Fact]
    public void WaitsForSessionCleanupOnDispose()
    {
        using var storeDir = StoreServiceTest.CreateSecureDirectory();
        string pipeName = _pipeName + ".Dispose";
        using var server = new StoreServiceServer([storeDir], pipeName, new());
        server.Start();

        using var client = new RawClient(pipeName);
        client.Hello(ValidDigest);
        client.Response().Result.Should().Be(StoreServiceResult.Ok);
        client.Op(StoreServiceOp.AddDirectory, "dir");
        client.Op(StoreServiceOp.AddFile, "dir/file");
        client.Writer.WriteInt64(0);
        client.Writer.WriteBool(false);
        client.Writer.WriteInt64(-1);
        client.Writer.WriteUInt32(4);
        client.Writer.Write([1, 2, 3, 4], 0, 4);
        for (int i = 0; i < 50 && !Directory.GetDirectories(storeDir, "0install-extract-*").Any(); i++)
            Thread.Sleep(100);
        Directory.GetDirectories(storeDir, "0install-extract-*").Should().NotBeEmpty(because: "session should be in progress");

        server.Dispose();

        Directory.GetDirectories(storeDir, "0install-extract-*").Should().BeEmpty(because: "Dispose() should wait for the session to clean up");
    }

    [Fact]
    public async Task IdentifiesClient()
    {
        string expectedName;
        using (var identity = WindowsIdentity.GetCurrent())
            expectedName = identity.Name;

        string pipeName = _pipeName + ".Identify";
        using var serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var connecting = serverPipe.WaitForConnectionAsync(TestContext.Current.CancellationToken);
        using var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);
        clientPipe.Connect(5000);
        await connecting;

        StoreServiceSecurity.GetClientName(serverPipe).Should().Be(expectedName);
    }

    [Fact]
    public void ClientEncodingRejectsInvalidStrings()
    {
        ((Action)(() => StoreServiceWriter.Encode(new string('a', 11), maxBytes: 10))).Should().Throw<IOException>().WithMessage("*maximum length*");
        ((Action)(() => StoreServiceWriter.Encode("lone\uD800surrogate", maxBytes: 100))).Should().Throw<IOException>().WithMessage("*not valid Unicode*");
    }

    [Fact]
    public void ServerTruncatesAndSanitizesMessages()
    {
        var stream = new MemoryStream();
        new StoreServiceWriter(stream).WriteResponse(new(StoreServiceResult.IOError, "lone\uD800surrogate" + new string('a', StoreServiceProtocol.MaxMessageLength)));

        stream.Position = 0;
        var response = new StoreServiceReader(stream).ReadResponse();
        response.Result.Should().Be(StoreServiceResult.IOError);
        response.Message.Should().StartWith("lone�surrogate");
    }

    [Fact]
    public void DisconnectsIdleClients()
    {
        using var storeDir = StoreServiceTest.CreateSecureDirectory();
        string pipeName = _pipeName + ".Idle";
        using var server = new StoreServiceServer([storeDir], pipeName, new() {IdleTimeout = TimeSpan.FromSeconds(1)});
        server.Start();

        using var client = new RawClient(pipeName);
        client.Hello(ValidDigest);
        client.Response().Result.Should().Be(StoreServiceResult.Ok);

        Thread.Sleep(TimeSpan.FromSeconds(3));

        client.Invoking(x => x.Response()).Should().Throw<IOException>();
        for (int i = 0; i < 50 && Directory.GetDirectories(storeDir, "0install-extract-*").Any(); i++)
            Thread.Sleep(100);
        Directory.GetDirectories(storeDir, "0install-extract-*").Should().BeEmpty();
    }

    [Fact]
    public void DisconnectsClientsExceedingSessionDuration()
    {
        using var storeDir = StoreServiceTest.CreateSecureDirectory();
        string pipeName = _pipeName + ".Duration";
        using var server = new StoreServiceServer([storeDir], pipeName, new()
        {
            IdleTimeout = TimeSpan.FromSeconds(1),
            MaxSessionDuration = TimeSpan.FromSeconds(4)
        });
        server.Start();

        var stopwatch = Stopwatch.StartNew();
        using var client = new RawClient(pipeName);
        client.Hello(ValidDigest);
        client.Response().Result.Should().Be(StoreServiceResult.Ok);
        client.Op(StoreServiceOp.AddDirectory);
        client.Writer.WriteUInt16(100); // Longer than the number of bytes that will be sent
        bool disconnected = false;
        for (int i = 0; i < 40; i++) // Up to 12s, well beyond the session deadline
        {
            Thread.Sleep(300);
            try
            {
                client.Writer.WriteByte((byte)'a');
            }
            catch (IOException)
            {
                disconnected = true;
                break;
            }
        }
        stopwatch.Stop();

        disconnected.Should().BeTrue(because: "the fixed deadline applies even while the client is making progress");
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(2), because: "progress resets the idle timeout");
    }

    [Fact]
    public void HandlesRandomGarbage()
    {
        var random = new Random(42);
        for (int i = 0; i < 20; i++)
        {
            var garbage = new byte[random.Next(1, 256)];
            random.NextBytes(garbage);
            using (var client = new RawClient(_pipeName))
            {
                try
                {
                    client.Hello(ValidDigest);
                    client.Response();
                    client.Writer.Write(garbage, 0, garbage.Length);
                }
                catch (IOException)
                {}
            }
        }

        AcceptsValidRequest(); // Server still healthy
    }
}
