// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Methods for verifying signatures and user trust.
    /// </summary>
    public class TrustManager : ITrustManager
    {
        #region Dependencies
        private readonly Config _config;
        private readonly IOpenPgp _openPgp;
        private readonly TrustDB _trustDB;
        private readonly IFeedCache _feedCache;
        private readonly ITaskHandler _handler;

        /// <summary>
        /// Creates a new trust manager.
        /// </summary>
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="openPgp">The OpenPGP-compatible system used to validate the signatures.</param>
        /// <param name="trustDB">A database of OpenPGP signature fingerprints the users trusts to sign <see cref="Feed"/>s coming from specific domains.</param>
        /// <param name="feedCache">Provides access to a cache of <see cref="Feed"/>s that were downloaded via HTTP(S).</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions.</param>
        public TrustManager(Config config, IOpenPgp openPgp, TrustDB trustDB, IFeedCache feedCache, ITaskHandler handler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _openPgp = openPgp ?? throw new ArgumentNullException(nameof(openPgp));
            _trustDB = trustDB ?? throw new ArgumentNullException(nameof(trustDB));
            _feedCache = feedCache ?? throw new ArgumentNullException(nameof(feedCache));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
        #endregion

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
        public ValidSignature CheckTrust(byte[] data, FeedUri uri, string? localPath = null)
        {
            #region Sanity checks
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (data == null) throw new ArgumentNullException(nameof(data));
            #endregion

            if (uri.IsFile) throw new UriFormatException(Resources.FeedUriLocal);

            var domain = new Domain(uri.Host);
            KeyImported:
            var signatures = FeedUtils.GetSignatures(_openPgp, data).ToList();

            foreach (var signature in signatures.OfType<ValidSignature>())
            {
                if (_trustDB.IsTrusted(signature.FormatFingerprint(), domain))
                    return signature;
            }

            foreach (var signature in signatures.OfType<ValidSignature>())
            {
                if (HandleNewKey(uri, signature.FormatFingerprint(), domain))
                    return signature;
            }

            foreach (var signature in signatures.OfType<MissingKeySignature>())
            {
                Log.Info("Missing key for " + signature.FormatKeyID());
                AcquireMissingKey(signature, uri, localPath);
                goto KeyImported;
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
                _trustDB.TrustKey(fingerprint, domain);
                try
                {
                    _trustDB.Save();
                }
                #region Error handling
                catch (Exception ex)
                {
                    Log.Error(ex);
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
            string keyInformation = GetKeyInformation(fingerprint, out bool goodVote) ?? Resources.NoKeyInfoServerData;

            // Automatically trust key for _new_ feeds if voted good by key server
            if (_config.AutoApproveKeys && goodVote && !_feedCache.Contains(uri))
            {
                Log.Info("Auto-approving key for " + uri.ToStringRfc());
                return true;
            }

            // Otherwise ask user
            return _handler.Ask(
                string.Format(Resources.AskKeyTrust, uri.ToStringRfc(), fingerprint, keyInformation, domain),
                defaultAnswer: false, alternateMessage: Resources.UntrustedKeys);
        }

        /// <summary>
        /// Retrieves information about a OpenPGP key from the <see cref="Config.KeyInfoServer"/>.
        /// </summary>
        /// <param name="fingerprint">The fingerprint of the key to check.</param>
        /// <param name="goodVote">Returns <c>true</c> if the server indicated that the key is trustworthy.</param>
        /// <returns>Human-readable information about the key or <c>null</c> if the server failed to provide a response.</returns>
        private string? GetKeyInformation(string fingerprint, out bool goodVote)
        {
            if (_config.KeyInfoServer == null)
            {
                goodVote = false;
                return null;
            }

            try
            {
                var keyInfoUri = new Uri(_config.KeyInfoServer, "key/" + fingerprint);
                Log.Info("Getting key information for " + fingerprint + " from: " + keyInfoUri);
                var xmlReader = XmlReader.Create(keyInfoUri.AbsoluteUri);
                _handler.CancellationToken.ThrowIfCancellationRequested();
                if (!xmlReader.ReadToFollowing("item"))
                {
                    goodVote = false;
                    return null;
                }

                goodVote = xmlReader.MoveToAttribute("vote") && (xmlReader.Value == "good");
                xmlReader.MoveToContent();
                return xmlReader.ReadElementContentAsString();
            }
            #region Error handling
            catch (XmlException ex)
            {
                Log.Error(string.Format(Resources.UnableToParseKeyInfo, fingerprint));
                Log.Error(ex);
                goodVote = false;
                return null;
            }
            catch (WebException ex)
            {
                Log.Error(string.Format(Resources.UnableToRetrieveKeyInfo, fingerprint));
                Log.Error(ex);
                goodVote = false;
                return null;
            }
            #endregion
        }

        /// <summary>
        /// Acquires the OpenPGP key file required to verify the given signature.
        /// </summary>
        /// <param name="signature">The signature that could not be verified yet.</param>
        /// <param name="uri">The URI the signed data originally came from.</param>
        /// <param name="localPath">The local file path the signed data came from. May be <c>null</c> for in-memory data.</param>
        /// <exception cref="WebException">A key file could not be downloaded from the internet.</exception>
        /// <exception cref="SignatureException">A downloaded key file is damaged.</exception>
        /// <exception cref="IOException">A problem occurs while writing trust configuration.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the trust configuration is not permitted.</exception>
        private void AcquireMissingKey(MissingKeySignature signature, Uri uri, string? localPath = null)
        {
            if (!string.IsNullOrEmpty(localPath))
            {
                string keyFile = Path.Combine(localPath, signature.FormatKeyID() + ".gpg");
                if (File.Exists(keyFile))
                {
                    Log.Info("Importing key file: " + keyFile);
                    _openPgp.ImportKey(File.ReadAllBytes(keyFile));
                    return;
                }
            }

            try
            {
                DownloadKey(new Uri(uri, signature.FormatKeyID() + ".gpg"));
            }
            catch (WebException ex) when (_config.FeedMirror != null)
            {
                Log.Warn(string.Format(Resources.UnableToLoadKeyFile, uri) + " " + Resources.TryingFeedMirror);
                try
                {
                    DownloadKey(new Uri(_config.FeedMirror.EnsureTrailingSlash().AbsoluteUri + "keys/" + signature.FormatKeyID() + ".gpg"));
                }
                catch (WebException)
                {
                    // Report the original problem instead of mirror errors
                    throw ex.PreserveStack();
                }
            }
        }

        /// <summary>
        /// Downloads and imports a remote key file.
        /// </summary>
        /// <exception cref="WebException">The key file could not be downloaded from the internet.</exception>
        /// <exception cref="SignatureException">The downloaded key file is damaged.</exception>
        /// <exception cref="IOException">A problem occurs while writing trust configuration.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the trust configuration is not permitted.</exception>
        private void DownloadKey(Uri keyUri)
        {
            var download = new DownloadMemory(keyUri);
            _handler.RunTask(download);
            try
            {
                _openPgp.ImportKey(download.GetData());
            }
            #region Error handling
            catch (InvalidDataException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new SignatureException(ex.Message, ex);
            }
            #endregion
        }
    }
}
