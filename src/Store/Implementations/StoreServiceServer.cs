// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text;
using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Accepts implementations from <see cref="ServiceImplementationSink"/>s via a named pipe and adds them to machine-wide implementation directories.
/// Intended to be hosted by a Windows service running as LocalSystem.
/// </summary>
/// <remarks>
/// Clients are untrusted. They send a stream of build operations which are validated and applied to a temporary directory.
/// The service calculates the manifest digest itself and only commits the implementation if it matches.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class StoreServiceServer : IDisposable
{
    private readonly string _pipeName;
    private readonly StoreServiceLimits _limits;
    private readonly IReadOnlyList<ImplementationSink> _sinks;
    private readonly SemaphoreSlim _sessionSlots;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ConcurrentDictionary<NamedPipeServerStream, bool> _activePipes = new();
    private Task? _acceptLoop;
    private bool _disposed;

    /// <summary>
    /// Creates a new Store Service server.
    /// </summary>
    /// <param name="directories">The implementation directories to add implementations to. Queried last-to-first for adding.</param>
    /// <exception cref="IOException">None of the <paramref name="directories"/> is usable.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform is not Windows.</exception>
    public StoreServiceServer(IEnumerable<string> directories)
        : this(directories, StoreServiceProtocol.PipeName, new())
    {}

    internal StoreServiceServer(IEnumerable<string> directories, string pipeName, StoreServiceLimits limits)
    {
        if (!WindowsUtils.IsWindowsNT) throw new PlatformNotSupportedException();

        _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        _sessionSlots = new(limits.MaxConcurrentSessions, limits.MaxConcurrentSessions);
        _sinks = GetSecureSinks(directories ?? throw new ArgumentNullException(nameof(directories)));
        if (_sinks.Count == 0) throw new IOException("None of the implementation directories can be used securely by the Store Service.");
    }

    private static List<ImplementationSink> GetSecureSinks(IEnumerable<string> directories)
    {
        var sinks = new List<ImplementationSink>();
        foreach (string directory in directories)
        {
            try
            {
                var sink = new ImplementationSink(directory);
                if (StoreServiceSecurity.IsWritableByUntrusted(sink.Path, out string? reason))
                    Log.Error($"Not using implementation directory {sink.Path} for Store Service because it is writable by untrusted users: {reason}");
                else sinks.Add(sink);
            }
            #region Error handling
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Log.Error($"Not using implementation directory {directory} for Store Service", ex);
            }
            #endregion
        }
        return sinks;
    }

    /// <summary>
    /// Starts accepting client connections in the background.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The named pipe is already owned by someone else.</exception>
    /// <exception cref="IOException">The named pipe could not be created.</exception>
    public void Start()
    {
        if (_acceptLoop != null) throw new InvalidOperationException("Already started.");

        // Create the first instance synchronously to report errors (e.g., pipe squatting) to the caller
        var pipe = CreatePipe();
        _acceptLoop = Task.Factory.StartNew(() => AcceptLoop(pipe), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>
    /// Accepts client connections. Always keeps a listening pipe instance, so the pipe name cannot be claimed by someone else while the service is running.
    /// Connected clients wait (without an idle timeout) until a session slot is available.
    /// </summary>
    /// <remarks>Runs on a dedicated thread, so a busy thread pool cannot delay accepting clients.</remarks>
    private void AcceptLoop(NamedPipeServerStream? listening)
    {
        var token = _cancellation.Token;
        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream? connected = null;
            try
            {
                listening ??= CreatePipe();
                listening.WaitForConnectionAsync(token).GetAwaiter().GetResult();
                connected = listening;
                listening = null;
                listening = CreatePipe();

                _sessionSlots.Wait(token);
                var session = connected;
                connected = null;
                _ = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        HandleSession(session);
                    }
                    finally
                    {
                        ClosePipe(session);
                        _sessionSlots.Release();
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                if (connected != null) ClosePipe(connected);
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
            {
                if (connected != null) ClosePipe(connected);
                if (token.IsCancellationRequested) break;
                Log.Warn("Store Service failed to accept connection", ex);

                // Replace a possibly broken listening instance, creating the new one first to avoid leaving the pipe name unclaimed
                var broken = listening;
                listening = null;
                try
                {
                    listening = CreatePipe();
                }
                #region Error handling
                catch (Exception createException) when (createException is IOException or UnauthorizedAccessException)
                {
                    Log.Error("Store Service failed to create named pipe", createException);
                }
                #endregion

                if (broken != null) ClosePipe(broken);
                if (listening == null && token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1)))
                    break;
            }
        }

        if (listening != null) ClosePipe(listening);
    }

    private void ClosePipe(NamedPipeServerStream pipe)
    {
        _activePipes.TryRemove(pipe, out _);
        pipe.Dispose();
    }

    /// <summary>
    /// Creates a new instance of the named pipe and verifies that nobody else controls it.
    /// </summary>
    private NamedPipeServerStream CreatePipe()
    {
        var security = StoreServiceSecurity.CreatePipeSecurity();
        const int bufferSize = 64 * 1024; // Allows error responses to be sent while the client is still writing
        int maxInstances = _limits.MaxConcurrentSessions + 3; // Sessions + client waiting for slot + listening + replacement
#if NETFRAMEWORK
        var pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, maxInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, bufferSize, bufferSize, security);
#else
        var pipe = NamedPipeServerStreamAcl.Create(_pipeName, PipeDirection.InOut, maxInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, bufferSize, bufferSize, security);
#endif
        try
        {
            // If someone else created the pipe first, we would have joined their instance with their ACL
            if (!StoreServiceSecurity.IsOwnedBySelf(pipe))
                throw new UnauthorizedAccessException($"The named pipe {_pipeName} was created by someone else.");
        }
        #region Error handling
        catch
        {
            pipe.Dispose();
            throw;
        }
        #endregion

        _activePipes[pipe] = true;
        return pipe;
    }

    /// <summary>
    /// How long <see cref="Dispose"/> waits for aborted sessions to clean up (or finish committing).
    /// Must stay well below the time the Windows Service Control Manager allows for stopping a service.
    /// </summary>
    private static readonly TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Stops accepting connections and aborts all active sessions.
    /// Waits briefly for the sessions to delete their temporary directories.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var stopwatch = Stopwatch.StartNew();
        TimeSpan Remaining()
        {
            var remaining = _shutdownTimeout - stopwatch.Elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        _cancellation.Cancel();
        foreach (var pipe in _activePipes.Keys)
        {
            try
            {
                pipe.Dispose();
            }
            catch (IOException)
            {}
        }
        try
        {
            _acceptLoop?.Wait(Remaining());
        }
        catch (AggregateException)
        {}

        // Each running session holds a slot until it has cleaned up
        for (int i = 0; i < _limits.MaxConcurrentSessions; i++)
        {
            if (!_sessionSlots.Wait(Remaining()))
            {
                Log.Warn("Store Service sessions did not finish in time during shutdown");
                break;
            }
        }

        _cancellation.Dispose();
    }

    private void HandleSession(NamedPipeServerStream pipe)
    {
        string client = StoreServiceSecurity.GetClientName(pipe);
        using var deadline = new DeadlineWatchdog(_limits.MaxSessionDuration, () =>
        {
            Log.Warn($"Store Service client {client} exceeded the session duration limit");
            pipe.Dispose();
        });
        using var watchdog = new IdleWatchdog(_limits.IdleTimeout, () =>
        {
            Log.Warn($"Store Service client {client} timed out");
            pipe.Dispose();
        });
        var reader = new StoreServiceReader(pipe, watchdog.Touch);
        var writer = new StoreServiceWriter(pipe);

        try
        {
            var response = Process(reader, writer, watchdog, client);
            watchdog.Resume();
            writer.WriteResponse(response);
            pipe.WaitForPipeDrain();
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Log.Debug($"Store Service client {client} disconnected", ex);
        }
        #endregion
    }

    private StoreServiceResponse Process(StoreServiceReader reader, StoreServiceWriter writer, IdleWatchdog watchdog, string client)
    {
        var digest = default(ManifestDigest);
        try
        {
            digest = ReadBegin(reader);

            if (_sinks.Any(x => x.Contains(digest)))
                return new(StoreServiceResult.AlreadyInStore);
            writer.WriteResponse(new(StoreServiceResult.Ok));

            Add(digest, reader, watchdog);
            Log.Info($"Store Service client {client} added implementation {digest.Best}");
            return new(StoreServiceResult.Ok);
        }
        #region Error handling
        catch (Exception ex) when (ex is EndOfStreamException or ObjectDisposedException)
        {
            throw new IOException("Client disconnected.", ex);
        }
        catch (Exception ex)
        {
            if (ex is IOException or UnauthorizedAccessException or InvalidDataException or StoreServiceLimitException or NotSupportedException or ArgumentException or DigestMismatchException or ImplementationAlreadyInStoreException)
                Log.Info($"Store Service client {client} failed to add implementation {digest.Best}: {StoreServicePaths.Escape(ex.Message)}"); // Message may contain paths sent by the client
            else Log.Error($"Store Service client {client} failed to add implementation {digest.Best}", ex);
            return ToResponse(ex);
        }
        #endregion
    }

    private static ManifestDigest ReadBegin(StoreServiceReader reader)
    {
        byte[] magic = new byte[StoreServiceProtocol.Magic.Length];
        reader.ReadExactly(magic, 0, magic.Length);
        if (!magic.SequenceEqual(StoreServiceProtocol.Magic)) throw new InvalidDataException("Unknown protocol.");

        ushort version = reader.ReadUInt16();
        if (version != StoreServiceProtocol.Version) throw new NotSupportedException($"Unsupported protocol version {version}.");

        int count = reader.ReadByte();
        if (count is 0 or > StoreServiceProtocol.MaxDigests) throw new InvalidDataException("Invalid number of digests.");
        string[] digests = new string[count];
        for (int i = 0; i < count; i++)
            digests[i] = reader.ReadString(StoreServiceProtocol.MaxDigestLength);

        return StoreServicePaths.ToStrongDigest(digests);
    }

    private void Add(ManifestDigest digest, StoreServiceReader reader, IdleWatchdog watchdog)
    {
        Exception? lastException = null;
        foreach (var sink in _sinks.Reverse())
        {
            bool started = false;
            try
            {
                sink.Add(digest, builder =>
                {
                    started = true;
                    Replay(reader, builder, _limits);
                    watchdog.Suspend(); // Client is now waiting for us to verify and commit
                });
                return;
            }
            catch (Exception ex) when (!started && ex is IOException or UnauthorizedAccessException)
            {
                // Try next sink, no data has been consumed from the client yet
                lastException = ex;
            }
        }
        throw lastException ?? new IOException("No implementation directory available.");
    }

    /// <summary>
    /// Reads build operations from the client and applies them to <paramref name="builder"/> until <see cref="StoreServiceOp.Commit"/> is received.
    /// </summary>
    private static void Replay(StoreServiceReader reader, IBuilder builder, StoreServiceLimits limits)
    {
        var quota = new StoreServiceQuota(limits);
        string ReadPath() => quota.AddPath(StoreServicePaths.ToNativePath(reader.ReadString(StoreServiceProtocol.MaxPathLength * 4)));

        while (true)
        {
            var op = (StoreServiceOp)reader.ReadByte();
            if (op == StoreServiceOp.Commit) return;
            quota.AddEntry();

            switch (op)
            {
                case StoreServiceOp.AddDirectory:
                    builder.AddDirectory(ReadPath());
                    break;

                case StoreServiceOp.AddFile:
                {
                    string path = ReadPath();
                    long modifiedTime = reader.ReadInt64();
                    if (modifiedTime is < StoreServiceProtocol.MinModifiedTime or > StoreServiceProtocol.MaxModifiedTime) throw new InvalidDataException("Invalid modification time.");
                    bool executable = reader.ReadBool();
                    long length = reader.ReadInt64();
                    if (length < -1) throw new InvalidDataException("Invalid file length.");
                    quota.CheckDeclaredSize(length);

                    var stream = new StoreServiceFileStream(reader, length, quota, limits.MaxPreallocatedBytes);
                    builder.AddFile(path, stream, modifiedTime, executable);
                    stream.ReadToEnd();
                    break;
                }

                case StoreServiceOp.AddSymlink:
                {
                    string path = ReadPath();
                    string target = quota.AddPath(StoreServicePaths.ValidateSymlinkTarget(reader.ReadString(StoreServiceProtocol.MaxSymlinkTargetLength)));
                    builder.AddSymlink(path, target);
                    break;
                }

                case StoreServiceOp.AddHardlink:
                {
                    string path = ReadPath();
                    string target = ReadPath();
                    bool executable = reader.ReadBool();
                    builder.AddHardlink(path, target, executable);
                    break;
                }

                case StoreServiceOp.Rename:
                {
                    string path = ReadPath();
                    string target = ReadPath();
                    builder.Rename(path, target);
                    break;
                }

                case StoreServiceOp.Remove:
                    builder.Remove(ReadPath());
                    break;

                case StoreServiceOp.MarkAsExecutable:
                    builder.MarkAsExecutable(ReadPath());
                    break;

                case StoreServiceOp.TurnIntoSymlink:
                    builder.TurnIntoSymlink(ReadPath());
                    break;

                default:
                    throw new InvalidDataException($"Unknown operation {(byte)op}.");
            }
        }
    }

    private static StoreServiceResponse ToResponse(Exception ex)
        => ex switch
        {
            StoreServiceLimitException => new(StoreServiceResult.LimitExceeded, ex.Message),
            InvalidDataException => new(StoreServiceResult.InvalidRequest, ex.Message),
            ImplementationAlreadyInStoreException => new(StoreServiceResult.AlreadyInStore, ex.Message),
            DigestMismatchException mismatch => new(StoreServiceResult.DigestMismatch, ex.Message)
            {
                ExpectedDigest = mismatch.ExpectedDigest,
                ActualDigest = mismatch.ActualDigest,
                ActualManifest = SerializeManifest(mismatch.ActualManifest)
            },
            NotSupportedException => new(StoreServiceResult.NotSupported, ex.Message),
            UnauthorizedAccessException => new(StoreServiceResult.AccessDenied, ex.Message),
            IOException => new(StoreServiceResult.IOError, ex.Message),
            ArgumentException => new(StoreServiceResult.InvalidRequest, ex.Message),
            _ => new(StoreServiceResult.IOError, "Internal error in Store Service.")
        };

    /// <summary>
    /// Serializes a manifest for sending to the client.
    /// </summary>
    /// <returns>The UTF-8 encoded manifest; empty if it is missing or longer than <see cref="StoreServiceProtocol.MaxManifestLength"/>.</returns>
    /// <remarks>Stops early instead of building the entire manifest in memory, since it may be very large.</remarks>
    internal static byte[] SerializeManifest(Manifest? manifest)
    {
        if (manifest == null) return [];

        var stream = new MemoryStream();
        foreach (string line in manifest.Lines)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
            if (stream.Length + bytes.Length > StoreServiceProtocol.MaxManifestLength) return [];
            stream.Write(bytes, 0, bytes.Length);
        }
        return stream.ToArray();
    }
}
