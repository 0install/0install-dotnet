// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Methods for verifying signatures and user trust.
/// </summary>
/// <remarks>Implementations of this interface are immutable and thread-safe.</remarks>
public interface ITrustManager
{
    /// <summary>
    /// Checks whether a remote feed or catalog file has a a valid and trusted signature. Downloads missing GPG keys for verification and interactively asks the user to approve new keys.
    /// </summary>
    /// <param name="data">The data of the file.</param>
    /// <param name="uri">The URI the <paramref name="data"/> originally came from.</param>
    /// <param name="keyCallback">Callback for reading a specific OpenPGP public key file.</param>
    /// <returns>The first valid and trusted signature found on the feed.</returns>
    /// <exception cref="UriFormatException"><paramref name="uri"/> is a local file.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="WebException">A key file could not be downloaded from the internet.</exception>
    /// <exception cref="SignatureException">No trusted signature was found.</exception>
    /// <exception cref="IOException">A problem occurred while writing trust configuration.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the trust configuration is not permitted.</exception>
    ValidSignature CheckTrust(byte[] data, FeedUri uri, OpenPgpKeyCallback? keyCallback = null);
}
