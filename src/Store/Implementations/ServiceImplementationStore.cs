// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using NanoByte.Common.Threading;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Forwards request to an <see cref="IImplementationSink"/> running in a Store Service via IPC.
/// </summary>
public partial class ServiceImplementationStore : IImplementationStore
{
    /// <inheritdoc/>
    public ImplementationStoreKind Kind => ImplementationStoreKind.Service;

    /// <summary>
    /// Always returns <c>false</c>. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
    /// </summary>
    /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
    public bool Contains(ManifestDigest manifestDigest) => false;

    /// <inheritdoc/>
    public void Add(ManifestDigest manifestDigest, Action<IBuilder> build)
    {
        try
        {
            GetProxy().Add(manifestDigest, build.ToMarshalByRef());
            Log.Info($"Sent implementation to Store Service: {manifestDigest.Best}");
        }
        #region Error handling
        catch (Exception ex) when (ex is RemotingException or SerializationException or TargetInvocationException)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(Resources.StoreServiceCommunicationProblem, ex);
        }
        #endregion
    }

    /// <inheritdoc/>
    public string Path => "-";

    /// <summary>
    /// Always returns <c>null</c>. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
    /// </summary>
    /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
    public string? GetPath(ManifestDigest manifestDigest) => null;

    /// <summary>
    /// Always returns empty list. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
    /// </summary>
    /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
    public IEnumerable<ManifestDigest> ListAll() => Enumerable.Empty<ManifestDigest>();

    /// <summary>
    /// Always returns empty list. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
    /// </summary>
    /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
    public IEnumerable<string> ListTemp() => Enumerable.Empty<string>();

    /// <inheritdoc/>
    public void Verify(ManifestDigest manifestDigest) => throw new NotSupportedException();

    /// <summary>
    /// Does nothing. Should be handled by an <see cref="ImplementationStore"/> directly instead of using the service.
    /// </summary>
    public bool Remove(ManifestDigest manifestDigest) => false;

    /// <summary>
    /// Does nothing. Should be handled by an <see cref="ImplementationStore"/> directly instead of using the service.
    /// </summary>
    public bool RemoveTemp(string path) => false;

    /// <summary>
    /// Does nothing. Should be handled by an <see cref="ImplementationStore"/> directly instead of using the service.
    /// </summary>
    public void Purge() {}

    /// <summary>
    /// Does nothing. Should be handled by an <see cref="ImplementationStore"/> directly instead of using the service.
    /// </summary>
    public long Optimise() => 0;

    /// <summary>
    /// Returns a fixed string.
    /// </summary>
    public override string ToString()
        => "Connection to Store Service (if available)";
}
#endif
