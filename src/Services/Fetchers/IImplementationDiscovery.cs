// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Fetchers;

/// <summary>
/// Discovers implementations in implementation stores on other machines in the local network.
/// </summary>
public interface IImplementationDiscovery
{
    /// <summary>
    /// Finds a specific implementation in an implementation store on another machine.
    /// Blocks until the implementation is found or the operation is cancelled.
    /// </summary>
    /// <param name="manifestDigest">The digest the implementation to find.</param>
    /// <param name="cancellationToken">Used to stop looking for the implementation.</param>
    /// <returns>An archive URI from which the implementation can be downloaded.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled.</exception>
    Uri GetImplementation(ManifestDigest manifestDigest, CancellationToken cancellationToken);
}
