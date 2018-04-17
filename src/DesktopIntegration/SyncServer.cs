// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Net;
using NanoByte.Common.Net;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Contains access information for a Sync server used by <see cref="SyncIntegrationManager"/>.
    /// </summary>
    public struct SyncServer
    {
        private Uri _uri;

        /// <summary>
        /// The base URI of the sync server. Automatically ensures the URI ends with a slash (/).
        /// </summary>
        public Uri Uri { get => _uri; set => _uri = value.EnsureTrailingSlash(); }

        /// <summary>
        /// The username to authenticate with against the server at <see cref="Uri"/>.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password to authenticate with against the server at <see cref="Uri"/>.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Combines <see cref="Username"/> and <see cref="Password"/>.
        /// </summary>
        public NetworkCredential Credentials => new NetworkCredential(Username, Password);

        /// <summary>
        /// Indicates whether the current settings are semantically valid.
        /// </summary>
        public bool IsValid
            => (Uri.IsFile && Uri.IsAbsoluteUri)
            || ((Uri.Scheme == Uri.UriSchemeHttp || Uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password));
    }
}
