// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Security.Principal;
using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Sends implementations to the Store Service (<see cref="StoreServiceServer"/>) via a named pipe, to add them to a machine-wide cache.
/// </summary>
/// <remarks>
/// Only available on Windows.
/// Only implementations identified by SHA-256 based digests are accepted.
/// </remarks>
public sealed class ServiceImplementationSink : IImplementationSink
{
    private readonly string _pipeName;
    private readonly IReadOnlyCollection<SecurityIdentifier>? _trustedOwners;
    private readonly TimeSpan _connectTimeout;

    /// <summary>
    /// Creates a new sink for sending implementations to the Store Service.
    /// </summary>
    public ServiceImplementationSink()
        : this(StoreServiceProtocol.PipeName, trustedOwners: null, TimeSpan.FromSeconds(10))
    {}

    internal ServiceImplementationSink(string pipeName, IReadOnlyCollection<SecurityIdentifier>? trustedOwners, TimeSpan connectTimeout)
    {
        _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        _trustedOwners = trustedOwners;
        _connectTimeout = connectTimeout;
    }

    /// <summary>
    /// Always returns <c>false</c>. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
    /// </summary>
    /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
    public bool Contains(ManifestDigest manifestDigest) => false;

    /// <inheritdoc/>
    public void Add(ManifestDigest manifestDigest, Action<IBuilder> build)
    {
        #region Sanity checks
        if (build == null) throw new ArgumentNullException(nameof(build));
        #endregion

        if (!WindowsUtils.IsWindowsNT) throw new IOException(Resources.StoreServiceCommunicationProblem, new PlatformNotSupportedException());
        if (manifestDigest.Sha256New == null && manifestDigest.Sha256 == null) throw new IOException("The Store Service only accepts implementations identified by SHA-256 based digests.");

        using var client = StoreServiceClient.Connect(_pipeName, _trustedOwners ?? StoreServiceSecurity.DefaultTrustedOwners, _connectTimeout);
        client.Begin(manifestDigest);
        build(new StoreServiceBuilder(client));
        client.Commit();

        Log.Info($"Sent implementation to Store Service: {manifestDigest.Best}");
    }

    /// <summary>
    /// Returns a fixed string.
    /// </summary>
    public override string ToString()
        => "Store Service";
}
