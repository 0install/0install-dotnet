// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Fetchers;


/// <summary>
/// Provides extension methods for <see cref="IImplementationDiscovery"/>.
/// </summary>
public static class ImplementationDiscoveryExtensions
{
    /// <summary>
    /// Tries to find a specific implementation in an implementation store on another machine within the specified <paramref name="timeout"/>.
    /// </summary>
    /// <param name="discovery">The implementation discovery service.</param>
    /// <param name="manifestDigest">The digest the implementation to find.</param>
    /// <param name="timeout">The amount of time to look for the implementation.</param>
    /// <param name="cancellationToken">Used to stop looking for the implementation.</param>
    /// <returns>An archive URI from which the implementation can be downloaded; <c>null</c> if the implementation was not found within the specified <paramref name="timeout"/>.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled.</exception>
    public static Uri? TryGetImplementation(this IImplementationDiscovery discovery, ManifestDigest manifestDigest, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return discovery.GetImplementation(manifestDigest, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
