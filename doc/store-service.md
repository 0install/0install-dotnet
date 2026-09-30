---
uid: store-service
---

# Store Service

On Windows, the Store Service lets unprivileged users add implementations to a machine-wide [implementation cache](https://docs.0install.net/details/cache/) (e.g., `%ProgramData%\0install.net\implementations`), which only the system and administrators can write to.

The service is hosted by a Windows service running as `LocalSystem` and implemented by <xref:ZeroInstall.Store.Implementations.StoreServiceServer>.  
Clients talk to it using <xref:ZeroInstall.Store.Implementations.ServiceImplementationSink>, which <xref:ZeroInstall.Store.Implementations.ImplementationStores.Default*> adds automatically on Windows when not running in portable mode.

This page specifies the wire protocol between the two.

## Design

- **Clients are untrusted.** Any authenticated local user can connect. The service treats everything a client sends as hostile input.
- **The service calculates the digest itself.** The client does not upload an archive or a manifest. It replays the individual <xref:ZeroInstall.Store.FileSystem.IBuilder> operations that construct the implementation. The service applies them to a temporary directory inside the target cache, calculates the manifest digest and only moves the directory into place if it matches the digest the client announced.
- **One connection adds one implementation.** There is no session state beyond a single connection.
- **Streaming with asynchronous errors.** The client sends all operations without waiting for per-operation acknowledgements. The service may report an error at any point and then close the connection.
- **Only SHA-256 based digests are accepted.** SHA-1 based digests are too weak to use as the only identity of content shared between users.

## Transport

| Property      | Value                                   |
| ------------- | --------------------------------------- |
| Transport     | Local Windows named pipe, byte mode     |
| Pipe name     | `\\.\pipe\ZeroInstall.Store.Service.v2` |
| Pipe buffers  | 64 KiB in each direction                |
| Max instances | `MaxConcurrentSessions + 3`             |

### Pipe security

The service creates every pipe instance with this DACL:

| Principal           | Access             |
| ------------------- | ------------------ |
| `NETWORK`           | Deny full control  |
| `SYSTEM`            | Allow full control |
| `Administrators`    | Allow full control |
| Service account     | Allow full control |
| Authenticated Users | Allow read/write   |

Authenticated Users are **not** granted `CreateNewInstance`, so they cannot add instances to the pipe and impersonate the service. Low-integrity processes are blocked by the default mandatory integrity label.

**Server-side squatting check:** After creating each instance, the service checks that the pipe's owner is its own account (or `Administrators` when running elevated). If someone else created the pipe first, the service joins their pipe object and inherits their ACL. In that case the owner check fails and the service refuses to start.

**Client-side squatting check:** Clients connect with `TokenImpersonationLevel.Identification`, so a rogue server cannot impersonate them. After connecting, a client checks that the pipe is owned by `SYSTEM` or `Administrators` and aborts otherwise.

Before connecting, clients call `WaitNamedPipe` with a 1 ms timeout. If the pipe does not exist (`ERROR_FILE_NOT_FOUND`), they fail immediately and do not wait for the connect timeout.

### Store directory security

On startup the service checks each configured cache directory. It refuses to use a directory whose owner is not `SYSTEM`, `Administrators`, `TrustedInstaller` or the service account, or whose DACL grants any other principal write, delete, change-permissions or take-ownership rights. `CREATOR OWNER` entries and inherit-only entries are ignored. The service fails to start if no directory is usable.

## Encoding

All integers are **little-endian**.

| Type     | Encoding                                                                                                                                                                           |
| -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `u8`     | 1 byte, unsigned                                                                                                                                                                   |
| `bool`   | `u8`, `0` = false, `1` = true. Any other value is a protocol error.                                                                                                                |
| `u16`    | 2 bytes, unsigned                                                                                                                                                                  |
| `u32`    | 4 bytes, unsigned                                                                                                                                                                  |
| `i64`    | 8 bytes, two's complement                                                                                                                                                          |
| `string` | `u16` byte length followed by that many bytes of UTF-8 without BOM. Invalid UTF-8 and `U+0000` are protocol errors. Each field has its own maximum length (see [Limits](#limits)). |
| `path`   | `string` holding a relative path with `/` as the separator. See [Path rules](#path-rules).                                                                                         |

## Message flow

```text
Client                                         Server
  |                                              |
  |  (connect)                                   |
  |                                              |  waits for a free session slot
  |  Hello ----------------------------------->  |
  |                                              |  validates digests, checks whether already in store
  |  <--------------------------------- Response |  Ok | AlreadyInStore | error
  |                                              |
  |  Operation*  ----------------------------->  |  applied to temp directory as received
  |  Commit  --------------------------------->  |
  |                                              |  verifies digest, moves into place
  |  <--------------------------------- Response |  Ok | DigestMismatch | error
  |                                              |
  |  (close)                                     |  (close)
```

- If the first response is anything other than `Ok`, the server closes the connection.
- While the client is sending operations, the server may send one error response at any time and then close the connection. So clients must read from the pipe concurrently while writing. Otherwise a large write can block forever on a server that stopped reading.
- There is exactly one response after `Hello` and at most one more response. The server never sends `Ok` except as these two responses.
- The server waits for the client to read the final response (`WaitForPipeDrain`) before closing.

### Hello

| Field       | Type                   | Notes                        |
| ----------- | ---------------------- | ---------------------------- |
| magic       | 4 bytes                | ASCII `0IS2` (`30 49 53 32`) |
| version     | `u16`                  | `1`                          |
| digestCount | `u8`                   | `1` to `4`                   |
| digests     | `digestCount * string` | Each at most 128 bytes       |

Each digest must match one of these patterns exactly:

| Algorithm   | Pattern                  |
| ----------- | ------------------------ |
| `sha256new` | `sha256new_[A-Z2-7]{52}` |
| `sha256`    | `sha256=[0-9a-f]{64}`    |
| `sha1new`   | `sha1new=[0-9a-f]{40}`   |
| `sha1`      | `sha1=[0-9a-f]{40}`      |

Any other digest string causes an `InvalidRequest` error. SHA-1 digests are accepted but ignored. The server uses the first `sha256new` digest, or else the first `sha256` digest. If there is neither, it responds with `NotSupported`.

If the implementation is already in any of the service's caches under the selected digest, the server responds with `AlreadyInStore`.

A wrong magic value is an `InvalidRequest`. An unknown version is `NotSupported`.

### Operations

Each operation starts with a `u8` opcode. The operations correspond 1:1 to <xref:ZeroInstall.Store.FileSystem.IBuilder> methods and have the same semantics.

| Opcode | Name               | Payload                                                                               |
| -----: | ------------------ | ------------------------------------------------------------------------------------- |
| `0x01` | `AddDirectory`     | `path path`                                                                           |
| `0x02` | `AddFile`          | `path path`, `i64 modifiedTime`, `bool executable`, `i64 length`, content (see below) |
| `0x03` | `AddSymlink`       | `path path`, `string target`                                                          |
| `0x04` | `AddHardlink`      | `path path`, `path target`, `bool executable`                                         |
| `0x05` | `Rename`           | `path path`, `path target`                                                            |
| `0x06` | `Remove`           | `path path`                                                                           |
| `0x07` | `MarkAsExecutable` | `path path`                                                                           |
| `0x08` | `TurnIntoSymlink`  | `path path`                                                                           |
| `0xFF` | `Commit`           | None. Ends the operation stream.                                                      |

Any other opcode is an `InvalidRequest`.

`IBuilder.TryAddExternalHardlink` has no wire representation. The client-side builder always returns `false`, so callers fall back to `AddFile`.

#### File content

`modifiedTime` is in seconds since the Unix epoch and must be between `-11644473600` (1601-01-01) and `253402300799` (9999-12-31).

`length` is the total content size in bytes, or `-1` if the client does not know it in advance. Values below `-1` are an `InvalidRequest`.

The content follows as a sequence of chunks:

```text
(u32 chunkLength, chunkLength bytes)*, u32 0
```

- `chunkLength` must not exceed 1 MiB. The reference client sends 64 KiB chunks.
- A chunk with length `0` ends the file.
- If `length` is not `-1`, the sum of all chunk lengths must equal `length` exactly.

#### Symlink targets

A symlink `target` must be non-empty, at most 4096 bytes and contain no characters below `U+0020`. It may be relative or absolute.

## Responses

| Field   | Type     | Notes                                             |
| ------- | -------- | ------------------------------------------------- |
| result  | `u8`     | See table below                                   |
| message | `string` | At most 4096 bytes. Human-readable. May be empty. |

If `result` is `DigestMismatch`, the response continues with:

| Field          | Type     | Notes                                                              |
| -------------- | -------- | ------------------------------------------------------------------ |
| expectedDigest | `string` | At most 128 bytes. The digest the client announced.                |
| actualDigest   | `string` | At most 128 bytes. The digest the service calculated.              |
| manifestLength | `u32`    | At most 4 MiB. `0` if the manifest was too large to send.          |
| manifest       | bytes    | The calculated manifest in UTF-8, in the format of `actualDigest`. |

| Code | Result           | Meaning                                                             | Client maps to                          |
| ---: | ---------------- | ------------------------------------------------------------------- | --------------------------------------- |
|  `0` | `Ok`             | Success                                                             | none                                    |
|  `1` | `AlreadyInStore` | The implementation is already in the cache                          | `ImplementationAlreadyInStoreException` |
|  `2` | `DigestMismatch` | The built implementation does not match the announced digest        | `DigestMismatchException`               |
|  `3` | `InvalidRequest` | Malformed or disallowed data                                        | `IOException`                           |
|  `4` | `IOError`        | An IO operation failed on the server, or an internal error occurred | `IOException`                           |
|  `5` | `AccessDenied`   | Access to a resource was denied on the server                       | `UnauthorizedAccessException`           |
|  `6` | `NotSupported`   | Unsupported protocol version or digest algorithm                    | `IOException`                           |
|  `7` | `LimitExceeded`  | A size or count limit was exceeded                                  | `IOException`                           |

Clients treat an unknown result code as a protocol error.

## Path rules

Paths are relative to the implementation root. The server rejects a path with `InvalidRequest` if any of these hold:

- It is empty or longer than 1024 UTF-16 code units.
- It has more than 64 components.
- Any component is empty (leading, trailing or doubled `/`), `.` or `..`.
- Any component is longer than 255 UTF-16 code units.
- Any component ends with `.` or a space, which Windows silently strips.
- Any component contains a control character (`U+0000` to `U+001F`, `U+007F`) or any of `< > : " | ? * \`.
- Any component's base name (the part before the first `.`, with trailing spaces removed) is a reserved DOS device name, case-insensitive: `CON`, `PRN`, `AUX`, `NUL`, `CONIN$`, `CONOUT$`, `CLOCK$`, `COM0`–`COM9`, `COM¹`–`COM³`, `LPT0`–`LPT9`, `LPT¹`–`LPT³`.

The <xref:ZeroInstall.Store.FileSystem.DirectoryBuilder> the operations are applied to adds further checks:

- An existing parent directory must not be a link.
- Hardlink targets must be existing regular files inside the implementation.
- Names reserved by the manifest format (`.manifest`, `.xbit`, `.symlink`) are rejected.
- Windows path normalization must not change the path. This rejects 8.3 short names of existing entries (e.g., `LONGFI~1.EXE`).
- Every existing entry along the path must have exactly the requested name. This rejects names that differ only in upper/lower case from an existing entry.

The last two rules matter because the manifest the service verifies is built from the operation stream and compares names exactly, while NTFS resolves names case-insensitively and via 8.3 short names. Without them, a client could build a manifest that matches the announced digest while the directory on disk has different content. For example, `AddDirectory t`, `AddDirectory T`, `AddFile T/f (good)`, `AddFile t/f (evil)`, `Rename T → A`, `AddDirectory t`, `Remove t` produces the manifest of `{A/f = good}`, but would leave `evil` in `A/f` on disk.

## Limits

The server enforces these limits for each connection. Exceeding one results in `LimitExceeded`, or `InvalidRequest` for malformed lengths.

| Limit                   | Default   | Notes                                                                                                                                                                      |
| ----------------------- | --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `MaxTotalBytes`         | 16 GiB    | Sum of all file content. A declared `length` that would exceed it is rejected up front.                                                                                    |
| `MaxPreallocatedBytes`  | 64 MiB    | Largest declared file `length` used to preallocate disk space. Larger files are written without preallocation, so a client cannot reserve disk space without sending data. |
| `MaxEntries`            | 1,000,000 | Number of operations, excluding `Commit`                                                                                                                                   |
| `MaxTotalPathLength`    | 32 Mi     | Sum of the lengths of all paths and symlink targets, in UTF-16 code units. Bounds the memory the service needs for the manifest.                                           |
| `IdleTimeout`           | 5 min     | Time without receiving data. Suspended while the server verifies and commits.                                                                                              |
| `MaxSessionDuration`    | 1 h       | Wall-clock time from the start of handling the session, including verification                                                                                             |
| `MaxConcurrentSessions` | 8         | Up to two additional clients can connect and wait for a slot, without an idle timeout. Further clients get `ERROR_PIPE_BUSY` until a pipe instance becomes available.      |

| Protocol constant         | Value                                                |
| ------------------------- | ---------------------------------------------------- |
| Max digests in `Hello`    | 4                                                    |
| Max digest length         | 128 bytes                                            |
| Max path length           | 1024 UTF-16 code units (4096 bytes on the wire)      |
| Max path component length | 255 UTF-16 code units                                |
| Max path depth            | 64                                                   |
| Max symlink target length | 4096 bytes                                           |
| Max message length        | 4096 bytes (the server truncates longer messages)    |
| Max manifest length       | 4 MiB (the server sends an empty manifest if longer) |
| Max chunk size            | 1 MiB                                                |

When the idle timeout or the session duration is exceeded, the server closes the connection without sending a response.

## Client behavior

The reference client (`ServiceImplementationSink`):

1. Fails immediately with `IOException` if the pipe does not exist, if not running on Windows, or if the digest has no SHA-256 based value.
2. Connects with a 10-second timeout and verifies the pipe owner.
3. Sends `Hello` and waits up to 10 seconds for the first response. This covers the time the server spends waiting for a free session slot. If the service stays busy for longer, the client gives up.
4. Replays the `IBuilder` calls made by the caller's build callback. It encodes all strings of an operation before writing any of it, and fails with an `IOException` naming the problem if a path or symlink target is too long or not valid Unicode. Before each write, it checks whether the server has already sent an error response. If a write fails, it waits up to 5 seconds for an error response and reports that error instead of a generic connection error.
5. Sends `Commit` and waits for the final response without a timeout. The server bounds this wait with its session duration limit.

<xref:ZeroInstall.Store.Implementations.CompositeImplementationSink> tries the Store Service first. If the service throws an `IOException` or `UnauthorizedAccessException`, it falls back to the user's own cache and invokes the build callback again.

## Versioning

The protocol is versioned in three places: the pipe name suffix (`.v2`), the magic (`0IS2`) and the `version` field (`1`). The legacy .NET Remoting based service used the pipe name `ZeroInstall.Store.Service`.

A server responds to an unknown `version` with `NotSupported`. Clients treat that like any other service failure and fall back to the user cache. There is no negotiation.
