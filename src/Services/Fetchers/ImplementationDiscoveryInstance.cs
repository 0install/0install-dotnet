// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Makaretu.Dns;
using NanoByte.Common.Threading;

namespace ZeroInstall.Services.Fetchers;

/// <summary>
/// An implementation store discovery by <see cref="ImplementationDiscovery"/>.
/// </summary>
[Equatable]
internal sealed partial class ImplementationDiscoveryInstance(ushort port, IEnumerable<IPAddress> potentialIPs, DomainName name)
{
    [IgnoreEquality]
    private IPAddress? _confirmedIP;

    /// <summary>
    /// The instance name.
    /// </summary>
    public DomainName Name { get; } = name;

    /// <summary>
    /// Tries to find a specific implementation in the implementation store.
    /// Blocks until the implementation is found or the operation is cancelled.
    /// </summary>
    /// <param name="manifestDigest">The digest the implementation to find.</param>
    /// <param name="cancellationToken">Used to stop looking for the implementation.</param>
    /// <returns>An archive URI from which the implementation can be downloaded.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled or the timeout was exceeded.</exception>
    public async Task<Uri?> GetImplementationAsync(ManifestDigest manifestDigest, CancellationToken cancellationToken)
        => _confirmedIP == null
            ? await ResultRacer.For(potentialIPs, (ip, innerCancellationToken) => TryGetUriAsync(ip, manifestDigest, innerCancellationToken), cancellationToken).GetResultAsync().ConfigureAwait(false)
            : await TryGetUriAsync(_confirmedIP, manifestDigest, cancellationToken).ConfigureAwait(false);

    private static readonly HttpClient _httpClient = new() {Timeout = TimeSpan.FromSeconds(2)};

    private async Task<Uri?> TryGetUriAsync(IPAddress ip, ManifestDigest manifestDigest, CancellationToken cancellationToken)
    {
        var uri = new UriBuilder {Scheme = "http", Host = ip.ToString(), Port = port, Path = $"{manifestDigest}.tar.gz"}.Uri;

        try
        {
            using var response = await _httpClient.SendAsync(new(HttpMethod.Head, uri), cancellationToken).ConfigureAwait(false);
            _confirmedIP = ip;
            if (response.IsSuccessStatusCode) return uri;
        }
        catch (HttpRequestException) {}

        return null;
    }
}
