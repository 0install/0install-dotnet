// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Resource limits enforced by <see cref="StoreServiceServer"/> for each client connection.
/// </summary>
internal sealed record StoreServiceLimits
{
    /// <summary>The maximum total size of all file contents in a single implementation.</summary>
    public long MaxTotalBytes { get; init; } = 16L * 1024 * 1024 * 1024;

    /// <summary>The maximum file size announced by a client that is used to preallocate disk space. Prevents clients from reserving large amounts of disk space without sending any data.</summary>
    public long MaxPreallocatedBytes { get; init; } = 64 * 1024 * 1024;

    /// <summary>The maximum number of build operations for a single implementation.</summary>
    public int MaxEntries { get; init; } = 1_000_000;

    /// <summary>The maximum total length of all paths and symlink targets in a single implementation, in UTF-16 code units. Bounds the memory used to build the manifest.</summary>
    public long MaxTotalPathLength { get; init; } = 32 * 1024 * 1024;

    /// <summary>The maximum time to wait for data from a client before disconnecting it.</summary>
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>The maximum duration of a client session before disconnecting it.</summary>
    public TimeSpan MaxSessionDuration { get; init; } = TimeSpan.FromHours(1);

    /// <summary>The maximum number of clients served concurrently.</summary>
    public int MaxConcurrentSessions { get; init; } = 8;
}

/// <summary>
/// Tracks resource usage of a single client connection against <see cref="StoreServiceLimits"/>.
/// </summary>
internal sealed class StoreServiceQuota(StoreServiceLimits limits)
{
    private long _bytes, _pathLength;
    private int _entries;

    /// <summary>
    /// Counts a build operation.
    /// </summary>
    /// <exception cref="StoreServiceLimitException">Too many operations.</exception>
    public void AddEntry()
    {
        if (++_entries > limits.MaxEntries) throw new StoreServiceLimitException($"Implementation exceeds maximum of {limits.MaxEntries} entries.");
    }

    /// <summary>
    /// Counts a received path or symlink target.
    /// </summary>
    /// <returns><paramref name="value"/> unchanged.</returns>
    /// <exception cref="StoreServiceLimitException">Too much path data.</exception>
    public string AddPath(string value)
    {
        _pathLength += value.Length;
        if (_pathLength > limits.MaxTotalPathLength) throw new StoreServiceLimitException($"Implementation exceeds maximum total path length of {limits.MaxTotalPathLength} characters.");
        return value;
    }

    /// <summary>
    /// Counts received file content.
    /// </summary>
    /// <exception cref="StoreServiceLimitException">Too much data.</exception>
    public void AddBytes(long count)
    {
        _bytes += count;
        if (_bytes > limits.MaxTotalBytes) throw new StoreServiceLimitException($"Implementation exceeds maximum size of {limits.MaxTotalBytes} bytes.");
    }

    /// <summary>
    /// Ensures that a file of a declared size would fit within the remaining quota.
    /// </summary>
    /// <exception cref="StoreServiceLimitException">The file would not fit.</exception>
    public void CheckDeclaredSize(long size)
    {
        if (size > limits.MaxTotalBytes - _bytes) throw new StoreServiceLimitException($"Implementation exceeds maximum size of {limits.MaxTotalBytes} bytes.");
    }
}

/// <summary>
/// Calls a callback when no activity has been reported for a specific time.
/// </summary>
internal sealed class IdleWatchdog : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly Action _onTimeout;
    private readonly Stopwatch _sinceActivity = Stopwatch.StartNew();
    private readonly Timer _timer;
    private volatile bool _suspended, _fired;

    /// <summary>
    /// Starts watching for inactivity.
    /// </summary>
    /// <param name="timeout">How long to wait for activity.</param>
    /// <param name="onTimeout">Called (at most once) when the timeout is exceeded.</param>
    public IdleWatchdog(TimeSpan timeout, Action onTimeout)
    {
        _timeout = timeout;
        _onTimeout = onTimeout;
        var period = TimeSpan.FromMilliseconds(Math.Max(10, Math.Min(1000, timeout.TotalMilliseconds / 4)));
        _timer = new Timer(_ => Check(), null, period, period);
    }

    /// <summary>
    /// Reports activity, resetting the timeout.
    /// </summary>
    public void Touch()
    {
        lock (_sinceActivity) _sinceActivity.Restart();
    }

    /// <summary>
    /// Stops the timeout from firing while the local side is busy.
    /// </summary>
    public void Suspend() => _suspended = true;

    /// <summary>
    /// Resumes watching for inactivity after <see cref="Suspend"/>.
    /// </summary>
    public void Resume()
    {
        Touch();
        _suspended = false;
    }

    private void Check()
    {
        if (_suspended || _fired) return;
        lock (_sinceActivity)
        {
            if (_sinceActivity.Elapsed <= _timeout) return;
        }

        _fired = true;
        try
        {
            _onTimeout();
        }
        catch (Exception ex)
        {
            Log.Debug("Idle timeout handler failed", ex);
        }
    }

    public void Dispose() => _timer.Dispose();
}

/// <summary>
/// Calls a callback when a fixed deadline is reached.
/// </summary>
internal sealed class DeadlineWatchdog : IDisposable
{
    private readonly Action _onTimeout;
    private readonly Timer _timer;
    private int _fired;

    public DeadlineWatchdog(TimeSpan timeout, Action onTimeout)
    {
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        _onTimeout = onTimeout;
        _timer = new Timer(_ => Fire(), null, timeout, Timeout.InfiniteTimeSpan);
    }

    private void Fire()
    {
        if (Interlocked.Exchange(ref _fired, 1) != 0) return;
        try
        {
            _onTimeout();
        }
        catch (Exception ex)
        {
            Log.Debug("Deadline timeout handler failed", ex);
        }
    }

    public void Dispose() => _timer.Dispose();
}

/// <summary>
/// Reads the chunked content of a single file sent by a client.
/// </summary>
/// <param name="reader">The reader for the underlying connection.</param>
/// <param name="declaredLength">The length announced by the client; -1 if unknown.</param>
/// <param name="quota">Tracks the total amount of data received.</param>
/// <param name="maxReportedLength">The maximum <paramref name="declaredLength"/> to report via <see cref="Length"/>. Consumers use it to preallocate disk space.</param>
internal sealed class StoreServiceFileStream(StoreServiceReader reader, long declaredLength, StoreServiceQuota quota, long maxReportedLength) : Stream
{
    private uint _chunkRemaining;
    private bool _ended;
    private long _total;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    /// <summary>The length announced by the client; -1 if unknown or larger than the maximum reported length.</summary>
    /// <remarks>Must not be clamped to a smaller value instead, because consumers such as <see cref="Manifests.ManifestBuilder"/> record it as the file size.</remarks>
    public override long Length { get; } = declaredLength <= maxReportedLength ? declaredLength : -1;

    public override long Position
    {
        get => _total;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_ended || count == 0) return 0;

        while (_chunkRemaining == 0)
        {
            uint next = reader.ReadUInt32();
            if (next == 0)
            {
                _ended = true;
                if (declaredLength >= 0 && _total != declaredLength) throw new InvalidDataException("File content does not match announced length.");
                return 0;
            }
            if (next > StoreServiceProtocol.MaxChunkSize) throw new InvalidDataException("Chunk too large.");
            _chunkRemaining = next;
        }

        int read = reader.ReadSome(buffer, offset, (int)Math.Min(count, _chunkRemaining));
        _chunkRemaining -= (uint)read;
        _total += read;
        quota.AddBytes(read);
        if (declaredLength >= 0 && _total > declaredLength) throw new InvalidDataException("File content exceeds announced length.");
        return read;
    }

    /// <summary>
    /// Consumes any content not read by the consumer, so the connection is positioned at the next operation.
    /// </summary>
    public void ReadToEnd()
    {
        var buffer = new byte[81920];
        while (Read(buffer, 0, buffer.Length) > 0) {}
    }

    public override void Flush() {}
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
