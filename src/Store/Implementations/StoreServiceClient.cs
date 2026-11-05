// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// A single connection from a client to the <see cref="StoreServiceServer"/>, used to add one implementation.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class StoreServiceClient : IDisposable
{
    private readonly NamedPipeClientStream _pipe;
    private readonly BufferedStream _output;
    private readonly StoreServiceWriter _writer;
    private readonly StoreServiceReader _reader;
    private readonly TimeSpan _availabilityTimeout;
    private Task<StoreServiceResponse>? _pendingResponse;
    private ManifestDigest _digest;

    private StoreServiceClient(NamedPipeClientStream pipe, TimeSpan availabilityTimeout)
    {
        _pipe = pipe;
        _output = new BufferedStream(pipe, StoreServiceProtocol.ClientChunkSize + 1024);
        _writer = new StoreServiceWriter(_output);
        _reader = new StoreServiceReader(pipe);
        _availabilityTimeout = availabilityTimeout;
    }

    /// <summary>
    /// Connects to the Store Service.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe to connect to.</param>
    /// <param name="trustedOwners">The accepted owners of the pipe. Protects against other users impersonating the service.</param>
    /// <param name="timeout">How long to wait for the service to become available if it is busy.</param>
    /// <exception cref="IOException">The service is not available or not trustworthy.</exception>
    public static StoreServiceClient Connect(string pipeName, IEnumerable<SecurityIdentifier> trustedOwners, TimeSpan timeout)
    {
        // Avoid waiting for the timeout if the service is not running at all
        if (!PipeExists(pipeName)) throw new IOException(Resources.StoreServiceCommunicationProblem);

        // Identification level prevents the server from impersonating us, in case someone else managed to create the pipe
        var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);
        try
        {
            pipe.Connect((int)timeout.TotalMilliseconds);
            if (!StoreServiceSecurity.IsOwnedBy(pipe, trustedOwners))
                throw new UnauthorizedAccessException($"The named pipe {pipeName} is not owned by a trusted account.");
        }
        #region Error handling
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            pipe.Dispose();
            throw new IOException(Resources.StoreServiceCommunicationProblem, ex);
        }
        #endregion

        return new(pipe, timeout);
    }

    private static bool PipeExists(string pipeName)
        => WaitNamedPipe(@"\\.\pipe\" + pipeName, 1) || Marshal.GetLastWin32Error() != ErrorFileNotFound;

    private const int ErrorFileNotFound = 2;

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WaitNamedPipe(string name, int timeout);

    /// <summary>
    /// Announces the implementation to be added and waits for the service to accept it.
    /// </summary>
    /// <exception cref="ImplementationAlreadyInStoreException">The implementation is already in the store.</exception>
    /// <exception cref="IOException">The service rejected the request.</exception>
    public void Begin(ManifestDigest digest)
    {
        _digest = digest;

        // The service ignores SHA-1 based digests
        var digests = new ManifestDigest(Sha256: digest.Sha256, Sha256New: digest.Sha256New)
                     .AvailableDigests
                     .Select(x => StoreServiceWriter.Encode(x, StoreServiceProtocol.MaxDigestLength))
                     .ToList();
        Send(writer =>
        {
            writer.Write(StoreServiceProtocol.Magic, 0, StoreServiceProtocol.Magic.Length);
            writer.WriteUInt16(StoreServiceProtocol.Version);
            writer.WriteByte((byte)digests.Count);
            foreach (byte[] value in digests)
                writer.WriteString(value);
        });
        Flush();

        _pendingResponse = StartReadingResponse();
        HandleResponse(WaitForResponse(_availabilityTimeout));

        // The service may report an error at any time while we are sending data
        _pendingResponse = StartReadingResponse();
    }

    /// <summary>
    /// Reads the next response on a dedicated thread, so a busy thread pool cannot delay it past the timeouts.
    /// </summary>
    private Task<StoreServiceResponse> StartReadingResponse()
        => Task.Factory.StartNew(ReadResponse, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    /// <summary>
    /// Sends data to the service.
    /// </summary>
    /// <exception cref="IOException">The service reported an error or the connection was lost.</exception>
    public void Send(Action<StoreServiceWriter> write)
    {
        ThrowIfErrorReported();
        try
        {
            write(_writer);
        }
        #region Error handling
        catch (IOException ex)
        {
            throw GetConnectionLostException(ex);
        }
        #endregion
    }

    private void Flush()
    {
        try
        {
            _output.Flush();
        }
        #region Error handling
        catch (IOException ex)
        {
            throw GetConnectionLostException(ex);
        }
        #endregion
    }

    /// <summary>
    /// Finishes the implementation and waits for the service to verify and store it.
    /// </summary>
    /// <exception cref="DigestMismatchException">The implementation does not match the digest.</exception>
    /// <exception cref="IOException">The service reported an error or the connection was lost.</exception>
    public void Commit()
    {
        Send(writer => writer.WriteByte((byte)StoreServiceOp.Commit));
        Flush();
        HandleResponse(WaitForResponse(Timeout.InfiniteTimeSpan));
    }

    private void ThrowIfErrorReported()
    {
        if (_pendingResponse is {IsCompleted: true})
            HandleResponse(WaitForResponse(TimeSpan.Zero), unexpected: true);
    }

    private Exception GetConnectionLostException(IOException ex)
    {
        // The service may have closed the connection after reporting an error
        if (_pendingResponse != null)
        {
            try
            {
                _pendingResponse.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {}

            if (_pendingResponse.Status == TaskStatus.RanToCompletion)
            {
                try
                {
                    HandleResponse(_pendingResponse.Result, unexpected: true);
                }
                catch (Exception reported)
                {
                    return reported;
                }
            }
        }

        return new IOException(Resources.StoreServiceCommunicationProblem, ex);
    }

    private StoreServiceResponse WaitForResponse(TimeSpan timeout)
    {
        var task = _pendingResponse ?? throw new InvalidOperationException("No response pending.");
        try
        {
            if (!task.Wait(timeout)) throw new IOException(Resources.StoreServiceCommunicationProblem);
            return task.Result;
        }
        #region Error handling
        catch (AggregateException ex)
        {
            throw ex.InnerException ?? ex;
        }
        #endregion
    }

    private StoreServiceResponse ReadResponse()
    {
        try
        {
            return _reader.ReadResponse();
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or InvalidDataException or StoreServiceLimitException or ObjectDisposedException)
        {
            throw new IOException(Resources.StoreServiceCommunicationProblem, ex);
        }
        #endregion
    }

    private void HandleResponse(StoreServiceResponse response, bool unexpected = false)
    {
        switch (response.Result)
        {
            case StoreServiceResult.Ok when !unexpected:
                return;
            case StoreServiceResult.Ok:
                throw new IOException(Resources.StoreServiceCommunicationProblem);
            case StoreServiceResult.AlreadyInStore:
                throw new ImplementationAlreadyInStoreException(_digest);
            case StoreServiceResult.DigestMismatch:
                throw new DigestMismatchException(response.ExpectedDigest, response.ActualDigest, actualManifest: TryParseManifest(response));
            case StoreServiceResult.AccessDenied:
                throw new UnauthorizedAccessException($"Store Service: {response.Message}");
            default:
                throw new IOException($"Store Service: {response.Message}");
        }
    }

    private static Manifest? TryParseManifest(StoreServiceResponse response)
    {
        if (response.ActualManifest is not {Length: > 0} || response.ActualDigest == null) return null;
        try
        {
            return Manifest.Load(new MemoryStream(response.ActualManifest), ManifestFormat.FromPrefix(response.ActualDigest));
        }
        #region Error handling
        catch (Exception ex) when (ex is FormatException or NotSupportedException or ArgumentException or IOException)
        {
            Log.Debug("Failed to parse manifest sent by Store Service", ex);
            return null;
        }
        #endregion
    }

    public void Dispose()
    {
        _pipe.Dispose();
        try
        {
            _pendingResponse?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {}
    }
}

/// <summary>
/// Sends build operations to the <see cref="StoreServiceServer"/>.
/// </summary>
/// <remarks>Strings are encoded before sending anything, so that invalid strings are reported as such rather than as a broken connection.</remarks>
[SupportedOSPlatform("windows")]
internal sealed class StoreServiceBuilder(StoreServiceClient client) : IBuilder
{
    public void AddDirectory(string path)
        => SendPathOp(StoreServiceOp.AddDirectory, path);

    public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
    {
        byte[] encodedPath = EncodePath(path);
        client.Send(writer =>
        {
            writer.WriteByte((byte)StoreServiceOp.AddFile);
            writer.WriteString(encodedPath);
            writer.WriteInt64(modifiedTime);
            writer.WriteBool(executable);
            writer.WriteInt64(stream.CanSeek ? stream.Length - stream.Position : -1);
        });

        byte[] buffer = new byte[StoreServiceProtocol.ClientChunkSize];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            client.Send(writer =>
            {
                writer.WriteUInt32((uint)read);
                writer.Write(buffer, 0, read);
            });
        }
        client.Send(writer => writer.WriteUInt32(0));
    }

    public void AddSymlink(string path, string target)
    {
        byte[] encodedPath = EncodePath(path);
        byte[] encodedTarget = StoreServiceWriter.Encode(target, StoreServiceProtocol.MaxSymlinkTargetLength);
        client.Send(writer =>
        {
            writer.WriteByte((byte)StoreServiceOp.AddSymlink);
            writer.WriteString(encodedPath);
            writer.WriteString(encodedTarget);
        });
    }

    public void AddHardlink(string path, string target, bool executable = false)
    {
        byte[] encodedPath = EncodePath(path);
        byte[] encodedTarget = EncodePath(target);
        client.Send(writer =>
        {
            writer.WriteByte((byte)StoreServiceOp.AddHardlink);
            writer.WriteString(encodedPath);
            writer.WriteString(encodedTarget);
            writer.WriteBool(executable);
        });
    }

    /// <summary>
    /// Always returns <c>false</c>. The service cannot access files outside of the implementation being built on behalf of clients.
    /// </summary>
    public bool TryAddExternalHardlink(string path, FileInfo target, bool executable = false)
        => false;

    public void Rename(string path, string target)
    {
        byte[] encodedPath = EncodePath(path);
        byte[] encodedTarget = EncodePath(target);
        client.Send(writer =>
        {
            writer.WriteByte((byte)StoreServiceOp.Rename);
            writer.WriteString(encodedPath);
            writer.WriteString(encodedTarget);
        });
    }

    public void Remove(string path)
        => SendPathOp(StoreServiceOp.Remove, path);

    public void MarkAsExecutable(string path)
        => SendPathOp(StoreServiceOp.MarkAsExecutable, path);

    public void TurnIntoSymlink(string path)
        => SendPathOp(StoreServiceOp.TurnIntoSymlink, path);

    private void SendPathOp(StoreServiceOp op, string path)
    {
        byte[] encodedPath = EncodePath(path);
        client.Send(writer =>
        {
            writer.WriteByte((byte)op);
            writer.WriteString(encodedPath);
        });
    }

    private static byte[] EncodePath(string path)
        => StoreServiceWriter.Encode(path.ToUnixPath()!, StoreServiceProtocol.MaxPathLength * 4);
}
