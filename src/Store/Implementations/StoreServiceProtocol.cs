// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Constants for the named-pipe protocol used by <see cref="ServiceImplementationSink"/> to talk to <see cref="StoreServiceServer"/>.
/// </summary>
/// <remarks>
/// All integers are little-endian. Strings are UTF-8, prefixed with their length in bytes as a <see cref="ushort"/>.
/// Paths use <c>/</c> as the separator on the wire.
/// <code>
/// Client: Magic, u16 Version, u8 digestCount, digestCount × string digest
/// Server: Response
/// Client: Op*, Commit
/// Server: Response
/// </code>
/// The server may send an error response at any time and then close the connection.
/// </remarks>
internal static class StoreServiceProtocol
{
    /// <summary>The name of the named pipe the Store Service listens on.</summary>
    public const string PipeName = "ZeroInstall.Store.Service.v2";

    /// <summary>Identifies the protocol at the start of a connection.</summary>
    public static readonly byte[] Magic = [.."0IS2"u8];

    /// <summary>The protocol version implemented by this code.</summary>
    public const ushort Version = 1;

    /// <summary>The maximum number of digests the client may send.</summary>
    public const int MaxDigests = 4;

    /// <summary>The maximum length of a single digest string in bytes.</summary>
    public const int MaxDigestLength = 128;

    /// <summary>The maximum length of a path in characters.</summary>
    public const int MaxPathLength = 1024;

    /// <summary>The maximum length of a single path component in characters.</summary>
    public const int MaxPathComponentLength = 255;

    /// <summary>The maximum number of components in a path.</summary>
    public const int MaxPathDepth = 64;

    /// <summary>The maximum length of a symlink target in bytes.</summary>
    public const int MaxSymlinkTargetLength = DirectoryBuilder.MaxSymlinkTargetSize;

    /// <summary>The maximum length of an error message in bytes.</summary>
    public const int MaxMessageLength = 4096;

    /// <summary>The maximum size of a manifest sent along with <see cref="StoreServiceResult.DigestMismatch"/>.</summary>
    public const int MaxManifestLength = 4 * 1024 * 1024;

    /// <summary>The maximum size of a single chunk of file content.</summary>
    public const int MaxChunkSize = 1024 * 1024;

    /// <summary>The chunk size used by the client when sending file content.</summary>
    public const int ClientChunkSize = 64 * 1024;

    /// <summary>The earliest file modification time accepted (1601-01-01, the earliest time NTFS can store).</summary>
    public const long MinModifiedTime = -11644473600;

    /// <summary>The latest file modification time accepted (9999-12-31).</summary>
    public const long MaxModifiedTime = 253402300799;

    /// <summary>UTF-8 encoding that throws on invalid byte sequences.</summary>
    public static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}

/// <summary>
/// Operations the client can send to build an implementation.
/// </summary>
internal enum StoreServiceOp : byte
{
    /// <summary>string path</summary>
    AddDirectory = 1,

    /// <summary>string path, i64 modifiedTime, u8 executable, i64 length (-1 if unknown), (u32 chunkLength, bytes)*, u32 0</summary>
    AddFile = 2,

    /// <summary>string path, string target</summary>
    AddSymlink = 3,

    /// <summary>string path, string target, u8 executable</summary>
    AddHardlink = 4,

    /// <summary>string path, string target</summary>
    Rename = 5,

    /// <summary>string path</summary>
    Remove = 6,

    /// <summary>string path</summary>
    MarkAsExecutable = 7,

    /// <summary>string path</summary>
    TurnIntoSymlink = 8,

    /// <summary>Finishes the implementation. Server verifies the digest and replies with a response.</summary>
    Commit = 0xFF
}

/// <summary>
/// Result codes sent by the server.
/// </summary>
/// <remarks>Each response is: u8 code, string message, followed by code-specific data.</remarks>
internal enum StoreServiceResult : byte
{
    /// <summary>The request was successful.</summary>
    Ok = 0,

    /// <summary>The implementation is already in the store.</summary>
    AlreadyInStore = 1,

    /// <summary>The implementation does not match the digest. Followed by: string expectedDigest, string actualDigest, u32 manifestLength, manifest bytes.</summary>
    DigestMismatch = 2,

    /// <summary>The client sent malformed or disallowed data.</summary>
    InvalidRequest = 3,

    /// <summary>An IO operation failed.</summary>
    IOError = 4,

    /// <summary>Access to a resource was denied.</summary>
    AccessDenied = 5,

    /// <summary>The request is valid but not supported by the service (e.g., weak digest algorithm, protocol version).</summary>
    NotSupported = 6,

    /// <summary>A size or count limit was exceeded.</summary>
    LimitExceeded = 7
}

/// <summary>
/// A size or count limit of the Store Service protocol was exceeded.
/// </summary>
internal sealed class StoreServiceLimitException(string message) : Exception(message);

/// <summary>
/// Reads protocol primitives from a stream, enforcing size limits.
/// </summary>
/// <param name="stream">The stream to read from.</param>
/// <param name="onActivity">Called whenever data was received.</param>
internal sealed class StoreServiceReader(Stream stream, Action? onActivity = null)
{
    private readonly byte[] _buffer = new byte[8];

    /// <summary>
    /// Reads up to <paramref name="count"/> bytes.
    /// </summary>
    /// <exception cref="EndOfStreamException">The connection was closed.</exception>
    public int ReadSome(byte[] buffer, int offset, int count)
    {
        int read = stream.Read(buffer, offset, count);
        if (read <= 0) throw new EndOfStreamException();
        onActivity?.Invoke();
        return read;
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes.
    /// </summary>
    /// <exception cref="EndOfStreamException">The connection was closed prematurely.</exception>
    public void ReadExactly(byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int read = ReadSome(buffer, offset, count);
            offset += read;
            count -= read;
        }
    }

    public byte ReadByte()
    {
        ReadExactly(_buffer, 0, 1);
        return _buffer[0];
    }

    public bool ReadBool()
        => ReadByte() switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException("Invalid boolean value.")
        };

    public ushort ReadUInt16()
    {
        ReadExactly(_buffer, 0, 2);
        return (ushort)(_buffer[0] | _buffer[1] << 8);
    }

    public uint ReadUInt32()
    {
        ReadExactly(_buffer, 0, 4);
        return (uint)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
    }

    public long ReadInt64()
    {
        ReadExactly(_buffer, 0, 8);
        ulong value = 0;
        for (int i = 7; i >= 0; i--)
            value = value << 8 | _buffer[i];
        return (long)value;
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string.
    /// </summary>
    /// <param name="maxBytes">The maximum permitted length in bytes.</param>
    /// <exception cref="InvalidDataException">The string is too long, not valid UTF-8 or contains NUL characters.</exception>
    public string ReadString(int maxBytes)
    {
        int length = ReadUInt16();
        if (length > maxBytes) throw new StoreServiceLimitException($"String exceeds maximum length of {maxBytes} bytes.");

        var bytes = new byte[length];
        ReadExactly(bytes, 0, length);

        string value;
        try
        {
            value = StoreServiceProtocol.Utf8.GetString(bytes);
        }
        #region Error handling
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException("String is not valid UTF-8.", ex);
        }
        #endregion

        if (value.IndexOf('\0') >= 0) throw new InvalidDataException("String contains NUL character.");
        return value;
    }

    /// <summary>
    /// Reads a response from the server.
    /// </summary>
    public StoreServiceResponse ReadResponse()
    {
        byte code = ReadByte();
        if (!Enum.IsDefined(typeof(StoreServiceResult), code)) throw new InvalidDataException($"Unknown result code {code}.");
        var response = new StoreServiceResponse((StoreServiceResult)code, ReadString(StoreServiceProtocol.MaxMessageLength));

        if (response.Result == StoreServiceResult.DigestMismatch)
        {
            response.ExpectedDigest = ReadString(StoreServiceProtocol.MaxDigestLength);
            response.ActualDigest = ReadString(StoreServiceProtocol.MaxDigestLength);
            uint manifestLength = ReadUInt32();
            if (manifestLength > StoreServiceProtocol.MaxManifestLength) throw new StoreServiceLimitException("Manifest too long.");
            byte[] manifest = new byte[manifestLength];
            ReadExactly(manifest, 0, manifest.Length);
            response.ActualManifest = manifest;
        }

        return response;
    }
}

/// <summary>
/// Writes protocol primitives to a stream.
/// </summary>
internal sealed class StoreServiceWriter(Stream stream)
{
    private readonly byte[] _buffer = new byte[8];

    public void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);

    public void WriteByte(byte value) => stream.WriteByte(value);

    public void WriteBool(bool value) => stream.WriteByte(value ? (byte)1 : (byte)0);

    public void WriteUInt16(ushort value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        stream.Write(_buffer, 0, 2);
    }

    public void WriteUInt32(uint value)
    {
        for (int i = 0; i < 4; i++)
            _buffer[i] = (byte)(value >> 8 * i);
        stream.Write(_buffer, 0, 4);
    }

    public void WriteInt64(long value)
    {
        for (int i = 0; i < 8; i++)
            _buffer[i] = (byte)((ulong)value >> 8 * i);
        stream.Write(_buffer, 0, 8);
    }

    /// <summary>
    /// Encodes a string for use with <see cref="WriteString(byte[])"/>.
    /// Call this before starting to write a message, so invalid strings do not leave a partially written message.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <param name="maxBytes">The maximum permitted length in bytes.</param>
    /// <exception cref="IOException"><paramref name="value"/> is too long or cannot be encoded as UTF-8 (e.g., contains unpaired surrogates).</exception>
    public static byte[] Encode(string value, int maxBytes)
    {
        byte[] bytes;
        try
        {
            bytes = StoreServiceProtocol.Utf8.GetBytes(value);
        }
        #region Error handling
        catch (EncoderFallbackException ex)
        {
            throw new IOException($"String is not valid Unicode: {StoreServicePaths.Escape(value)}", ex);
        }
        #endregion

        if (bytes.Length > maxBytes) throw new IOException($"String exceeds maximum length of {maxBytes} bytes: {value}");
        return bytes;
    }

    /// <summary>
    /// Writes a length-prefixed UTF-8 string.
    /// </summary>
    /// <param name="encoded">The string encoded using <see cref="Encode"/>.</param>
    public void WriteString(byte[] encoded)
    {
        WriteUInt16((ushort)encoded.Length);
        stream.Write(encoded, 0, encoded.Length);
    }

    /// <summary>
    /// Writes a length-prefixed UTF-8 string.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <param name="maxBytes">The maximum permitted length in bytes.</param>
    /// <param name="truncate"><c>true</c> to silently truncate overlong strings and replace unencodable characters; <c>false</c> to throw an exception.</param>
    /// <exception cref="IOException"><paramref name="value"/> is too long or cannot be encoded.</exception>
    public void WriteString(string value, int maxBytes, bool truncate = false)
        => WriteString(truncate ? EncodeTruncated(value, maxBytes) : Encode(value, maxBytes));

    /// <summary>UTF-8 encoding that replaces unpaired surrogates instead of throwing.</summary>
    private static readonly Encoding _lenientUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private static byte[] EncodeTruncated(string value, int maxBytes)
    {
        byte[] bytes = _lenientUtf8.GetBytes(value);
        if (bytes.Length <= maxBytes) return bytes;

        int length = Math.Min(value.Length, maxBytes);
        while (length > 0 && (char.IsHighSurrogate(value[length - 1]) || _lenientUtf8.GetByteCount(value.ToCharArray(0, length)) > maxBytes))
            length--;
        return _lenientUtf8.GetBytes(value[..length]);
    }

    /// <summary>
    /// Writes a response to the client.
    /// </summary>
    public void WriteResponse(StoreServiceResponse response)
    {
        WriteByte((byte)response.Result);
        WriteString(response.Message, StoreServiceProtocol.MaxMessageLength, truncate: true);

        if (response.Result == StoreServiceResult.DigestMismatch)
        {
            WriteString(response.ExpectedDigest ?? "", StoreServiceProtocol.MaxDigestLength, truncate: true);
            WriteString(response.ActualDigest ?? "", StoreServiceProtocol.MaxDigestLength, truncate: true);
            byte[] manifest = response.ActualManifest is {Length: <= StoreServiceProtocol.MaxManifestLength} ? response.ActualManifest : [];
            WriteUInt32((uint)manifest.Length);
            stream.Write(manifest, 0, manifest.Length);
        }

        stream.Flush();
    }
}

/// <summary>
/// A response sent by the server.
/// </summary>
internal sealed class StoreServiceResponse(StoreServiceResult result, string message = "")
{
    public StoreServiceResult Result { get; } = result;
    public string Message { get; } = message;

    /// <summary>
    /// For <see cref="StoreServiceResult.DigestMismatch"/>: the digest the client claimed.
    /// </summary>
    public string? ExpectedDigest { get; set; }

    /// <summary>
    /// For <see cref="StoreServiceResult.DigestMismatch"/>: the digest calculated by the service.
    /// </summary>
    public string? ActualDigest { get; set; }

    /// <summary>
    /// For <see cref="StoreServiceResult.DigestMismatch"/>: the UTF-8 manifest calculated by the service.
    /// </summary>
    public byte[]? ActualManifest { get; set; }
}
