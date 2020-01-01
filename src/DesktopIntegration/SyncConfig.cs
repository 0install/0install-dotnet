// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
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
        [NotNull]
        public Uri Uri { get; }

        /// <summary>
        /// The credentials to authenticate with against the sync server. Should only be <c>null</c> for testing.
        /// </summary>
        [CanBeNull]
        public ICredentials Credentials { get; }

        /// <summary>
        /// The local key used to encrypt data before sending it to the sync server. Should only be <c>null</c> for testing.
        /// </summary>
        [CanBeNull]
        public string CryptoKey { get; }

        /// <summary>
        /// Creates a new sync configuration.
        /// </summary>
        /// <param name="uri">The base URI of the sync server. Automatically ensures the URI ends with a slash (/).</param>
        /// <param name="credentials">The credentials to authenticate with against the sync server. Should only be <c>null</c> for testing.</param>
        /// <param name="cryptoKey">The local key used to encrypt data before sending it to the sync server. Should only be <c>null</c> for testing.</param>
        public SyncConfig([NotNull] Uri uri, [CanBeNull] ICredentials credentials = null, [CanBeNull] string cryptoKey = null)
        {
            Uri = (uri ?? throw new ArgumentNullException(nameof(uri))).EnsureTrailingSlash();
            Credentials = credentials;
            CryptoKey = cryptoKey;
        }

        /// <summary>
        /// Creates sync configuration from <paramref name="config"/> options.
        /// </summary>
        /// <exception cref="InvalidDataException">Not all required sync options are set.</exception>
        [NotNull]
        public static SyncConfig From([NotNull] Config config)
        {
            if (config.SyncServer == null || string.IsNullOrEmpty(config.SyncServerUsername) || string.IsNullOrEmpty(config.SyncServerPassword) || string.IsNullOrEmpty(config.SyncCryptoKey))
                throw new InvalidDataException(Resources.PleaseConfigSync);

            return new SyncConfig(
                config.SyncServer,
                new NetworkCredential(config.SyncServerUsername, config.SyncServerPassword),
                config.SyncCryptoKey);
        }
    }
}
