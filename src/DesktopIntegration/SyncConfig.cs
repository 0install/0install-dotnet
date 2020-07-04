// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using NanoByte.Common.Net;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Configuration for communicating with a sync server.
    /// </summary>
    public class SyncConfig
    {
        /// <summary>
        /// The base URI of the sync server.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// The credentials to authenticate with against the sync server. Should only be <c>null</c> for testing.
        /// </summary>
        public ICredentials? Credentials { get; }

        /// <summary>
        /// The local key used to encrypt data before sending it to the sync server. Should only be <c>null</c> for testing.
        /// </summary>
        public string? CryptoKey { get; }

        /// <summary>
        /// Creates a new sync configuration.
        /// </summary>
        /// <param name="uri">The base URI of the sync server. Automatically ensures the URI ends with a slash (/).</param>
        /// <param name="credentials">The credentials to authenticate with against the sync server. Should only be <c>null</c> for testing.</param>
        /// <param name="cryptoKey">The local key used to encrypt data before sending it to the sync server. Should only be <c>null</c> for testing.</param>
        public SyncConfig(Uri uri, ICredentials? credentials = null, string? cryptoKey = null)
        {
            Uri = (uri ?? throw new ArgumentNullException(nameof(uri))).EnsureTrailingSlash();
            Credentials = credentials;
            CryptoKey = cryptoKey;
        }

        /// <summary>
        /// Creates sync configuration from <paramref name="config"/> options.
        /// </summary>
        /// <exception cref="InvalidDataException">Not all required sync options are set.</exception>
        public static SyncConfig From(Config config)
        {
            if (config.SyncServer == null)
                throw new InvalidDataException(Resources.PleaseConfigSync);
            if (!config.SyncServer.IsFile && (string.IsNullOrEmpty(config.SyncServerUsername) || string.IsNullOrEmpty(config.SyncServerPassword) || string.IsNullOrEmpty(config.SyncCryptoKey)))
                throw new InvalidDataException(Resources.PleaseConfigSync);

            return new SyncConfig(
                config.SyncServer,
                new NetworkCredential(config.SyncServerUsername, config.SyncServerPassword),
                config.SyncCryptoKey);
        }
    }
}
