// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Methods for verifying signatures and user trust.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class TrustManager(TrustDB trustDB, Config config, IOpenPgp openPgp, IFeedCache feedCache, ITaskHandler handler) : ITrustManager
{
#if NET9_0_OR_GREATER
    private static readonly Lock _lock = new();
#else
    private static readonly object _lock = new();
#endif

    /// <inheritdoc/>
    public ValidSignature CheckTrust(byte[] data, FeedUri uri, OpenPgpKeyCallback? keyCallback = null)
    {
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (uri.IsFile) throw new UriFormatException(Resources.FeedUriLocal);

        var domain = new Domain(uri.Host);
        lock (_lock)
        {
            KeyImported:
            var signatures = FeedUtils.GetSignatures(openPgp, data).ToList();

            foreach (var signature in signatures.OfType<ValidSignature>())
            {
                if (trustDB.IsTrusted(signature.FormatFingerprint(), domain))
                    return signature;
            }

            foreach (var signature in signatures.OfType<ValidSignature>())
            {
                if (HandleNewKey(uri, signature.FormatFingerprint(), domain))
                    return signature;
            }

            foreach (var signature in signatures.OfType<MissingKeySignature>())
            {
                Log.Info($"Missing key for {signature.FormatKeyID()}");
                string id = signature.FormatKeyID();

                try
                {
                    openPgp.ImportKey(keyCallback?.Invoke(id) ?? DownloadKey(id, uri));
                }
                #region Error handling
                catch (InvalidDataException ex)
                {
                    // Wrap exception since only certain exception types are allowed
                    throw new SignatureException(ex.Message, ex);
                }
                #endregion
                goto KeyImported;
            }
        }

        throw new SignatureException(string.Format(Resources.FeedNoTrustedSignatures, uri));
    }

    /// <summary>
    /// Handles new keys that have not been trusted yet.
    /// </summary>
    /// <param name="uri">The URI the signed file originally came from.</param>
    /// <param name="fingerprint">The fingerprint of the key to trust.</param>
    /// <param name="domain">The domain to trust the key for.</param>
    /// <returns><c>true</c> if the user decided to trust the key, <c>false</c> if they decided not to trust the key.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    private bool HandleNewKey(FeedUri uri, string fingerprint, Domain domain)
    {
        if (AskKeyApproval(uri, fingerprint, domain))
        {
            trustDB.TrustKey(fingerprint, domain);
            try
            {
                trustDB.Save();
            }
            #region Error handling
            catch (Exception ex)
            {
                Log.Error(Resources.ErrorSavingTrustDB, ex);
            }
            #endregion
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Asks the user whether they trust a given key for a specific domain. May automatically accept based on policy.
    /// </summary>
    /// <param name="uri">The URI the signed file originally came from.</param>
    /// <param name="fingerprint">The fingerprint of the key to trust.</param>
    /// <param name="domain">The domain to trust the key for.</param>
    /// <returns><c>true</c> if the user decided to trust the key, <c>false</c> if they decided not to trust the key.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    private bool AskKeyApproval(FeedUri uri, string fingerprint, Domain domain)
    {
        (bool goodVote, string? keyInformation) = GetKeyInformation(fingerprint);

        // Automatically trust key for _new_ feeds if voted good by key server
        if (config.AutoApproveKeys && goodVote && !feedCache.Contains(uri))
        {
            Log.Info($"Auto-approving key for {uri.ToStringRfc()}");
            return true;
        }

        // Otherwise ask user
        return handler.Ask(
            string.Format(Resources.AskKeyTrust, uri.ToStringRfc(), fingerprint, keyInformation ?? Resources.NoKeyInfoServerData, domain),
            defaultAnswer: false, alternateMessage: Resources.UntrustedKeys);
    }

    /// <summary>
    /// Retrieves information about a OpenPGP key from the <see cref="Config.KeyInfoServer"/>.
    /// </summary>
    /// <param name="fingerprint">The fingerprint of the key to check.</param>
    /// <returns>Indication whether server considers the key is trustworthy, plus human-readable information about the key if available.</returns>
    private (bool goodVode, string? keyInformation) GetKeyInformation(string fingerprint)
    {
        if (config.KeyInfoServer == null)
            return (false, null);

        try
        {
            var keyInfoUri = new Uri(config.KeyInfoServer, $"key/{fingerprint}");
            Log.Info($"Getting key information for {fingerprint} from: {keyInfoUri}");
            var xmlReader = XmlReader.Create(keyInfoUri.AbsoluteUri);
            handler.CancellationToken.ThrowIfCancellationRequested();
            if (!xmlReader.ReadToFollowing("item"))
                return (false, null);

            bool goodVote = xmlReader.MoveToAttribute("vote") && (xmlReader.Value == "good");
            xmlReader.MoveToContent();
            string keyInformation = xmlReader.ReadElementContentAsString();
            return (goodVote, keyInformation);
        }
        #region Error handling
        catch (XmlException ex)
        {
            Log.Error(string.Format(Resources.UnableToParseKeyInfo, fingerprint), ex);
            return (false, null);
        }
        catch (WebException ex)
        {
            Log.Error(string.Format(Resources.UnableToRetrieveKeyInfo, fingerprint), ex);
            return (false, null);
        }
        #endregion
    }

    /// <summary>
    /// Downloads an OpenPGP key file from a location relative to a feed.
    /// </summary>
    /// <param name="id">The ID of the key to download.</param>
    /// <param name="feedUri">The URI of the feed next to which the key file is located.</param>
    /// <exception cref="WebException">The key file could not be downloaded from the internet.</exception>
    private ArraySegment<byte> DownloadKey(string id, Uri feedUri)
    {
        var keyUri = new Uri(feedUri, $"{id}.gpg");

        if (config.NetworkUse == NetworkLevel.Offline)
            throw new WebException(string.Format(Resources.NoDownloadInOfflineMode, keyUri));

        ArraySegment<byte> Download(Uri uri)
        {
            ArraySegment<byte> data = default;
            handler.RunTask(new DownloadFile(uri, stream => data = stream.ReadAll()));
            return data;
        }

        try
        {
            return Download(keyUri);
        }
        catch (WebException ex) when (config.FeedMirror != null && ex.ShouldTryMirror(keyUri))
        {
            Log.Warn(string.Format(Resources.TryingFeedMirror, keyUri));
            try
            {
                return Download(new($"{config.FeedMirror.EnsureTrailingSlash().AbsoluteUri}keys/{id}.gpg"));
            }
            catch (WebException ex2)
            {
                Log.Debug($"Failed to download GnuPG key {id} from feed mirror.", ex2);
                throw ex.Rethrow(); // Report the original problem instead of mirror errors
            }
        }
    }
}
