// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Methods for verifying signatures and user trust.
    /// </summary>
    public interface ITrustManager
    {
        /// <summary>
        /// Checks whether a remote feed or catalog file has a a valid and trusted signature. Downloads missing GPG keys for verification and interactively asks the user to approve new keys.
        /// </summary>
        /// <param name="data">The data of the file.</param>
        /// <param name="uri">The URI the <paramref name="data"/> originally came from.</param>
        /// <param name="localPath">The local file path the <paramref name="data"/> came from. Used to locate key files. May be <c>null</c> for in-memory data.</param>
        /// <returns>The first valid and trusted signature found on the feed.</returns>
        /// <exception cref="UriFormatException"><paramref name="uri"/> is a local file.</exception>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A key file could not be downloaded from the internet.</exception>
        /// <exception cref="SignatureException">No trusted signature was found.</exception>
        /// <exception cref="IOException">A problem occurs while writing trust configuration.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the trust configuration is not permitted.</exception>
        ValidSignature CheckTrust(byte[] data, FeedUri uri, string? localPath = null);
    }
}
