// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Net;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store
{
    /// <summary>
    /// User settings controlling network behaviour, solving, etc.
    /// </summary>
    [Serializable]
    public sealed partial class Config : ICloneable<Config>, IEquatable<Config>
    {
        private static readonly TimeSpan _defaultFreshness = TimeSpan.FromDays(7);

        /// <summary>
        /// The maximum age a cached <see cref="Feed"/> may have until it is considered stale (needs to be updated).
        /// </summary>
        [DefaultValue(typeof(TimeSpan), "7.00:00:00"), Category("Policy"), DisplayName(@"Freshness"), Description("The maximum age a cached feed may have until it is considered stale (needs to be updated).")]
        public TimeSpan Freshness { get; set; } = _defaultFreshness;

        /// <summary>
        /// Always prefer the newest versions, even if they have not been marked as <see cref="Stability.Stable"/> yet.
        /// </summary>
        [DefaultValue(false), Category("Policy"), DisplayName(@"Help with testing"), Description("Always prefer the newest versions, even if they have not been marked as stable yet.")]
        public bool HelpWithTesting { get; set; }

        private NetworkLevel _networkLevel = NetworkLevel.Full;

        /// <summary>
        /// Controls how liberally network access is attempted.
        /// </summary>
        [DefaultValue(typeof(NetworkLevel), "Full"), Category("Policy"), DisplayName(@"Network use"), Description("Controls how liberally network access is attempted.")]
        public NetworkLevel NetworkUse
        {
            get { return _networkLevel; }
            set
            {
                #region Sanity checks
                if (!Enum.IsDefined(typeof(NetworkLevel), value)) throw new ArgumentOutOfRangeException(nameof(value));
                #endregion

                _networkLevel = value;
            }
        }

        /// <summary>
        /// Automatically approve keys known by the <see cref="KeyInfoServer"/> and seen the first time a feed is fetched.
        /// </summary>
        [DefaultValue(true), Category("Policy"), DisplayName(@"Auto approve keys"), Description("Automatically approve keys known by the key info server and seen the first time a feed is fetched.")]
        public bool AutoApproveKeys { get; set; } = true;

        /// <summary>
        /// The default value for <see cref="FeedMirror"/>.
        /// </summary>
        public const string DefaultFeedMirror = "http://roscidus.com/0mirror";

        private Uri _feedMirror = new Uri(DefaultFeedMirror);

        /// <summary>
        /// The mirror server used to provide feeds when the original server is unavailable.
        /// </summary>
        [DefaultValue(typeof(Uri), DefaultFeedMirror), CanBeNull, Category("Sources"), DisplayName(@"Feed mirror"), Description("The mirror server used to provide feeds when the original server is unavailable.")]
        public Uri FeedMirror { get => _feedMirror; set => _feedMirror = value?.ReparseAsAbsolute(); }

        /// <summary>
        /// The default value for <see cref="KeyInfoServer"/>.
        /// </summary>
        public const string DefaultKeyInfoServer = "https://keylookup.appspot.com/";

        private Uri _keyInfoServer = new Uri(DefaultKeyInfoServer);

        /// <summary>
        /// The key information server used to get information about who signed a feed.
        /// </summary>
        [DefaultValue(typeof(Uri), DefaultKeyInfoServer), CanBeNull, Category("Sources"), DisplayName(@"Key info server"), Description("The key information server used to get information about who signed a feed.")]
        public Uri KeyInfoServer { get => _keyInfoServer; set => _keyInfoServer = value?.ReparseAsAbsolute(); }

        /// <summary>
        /// The default value for <see cref="SelfUpdateUri"/>.
        /// </summary>
        public const string DefaultSelfUpdateUri = "http://0install.de/feeds/ZeroInstall.xml";

        /// <summary>
        /// The feed URI used by the solver to search for updates for Zero Install itself.
        /// </summary>
        [DefaultValue(typeof(FeedUri), DefaultSelfUpdateUri), Category("Sources"), DisplayName(@"Self-update URI"), Description("The feed URI used by the solver to search for updates for Zero Install itself.")]
        public FeedUri SelfUpdateUri { get; set; } = new FeedUri(DefaultSelfUpdateUri);

        /// <summary>
        /// The default value for <see cref="ExternalSolverUri"/>.
        /// </summary>
        public const string DefaultExternalSolverUri = "http://0install.net/tools/0install.xml";

        /// <summary>
        /// The feed URI used to get the external solver.
        /// </summary>
        [DefaultValue(typeof(FeedUri), DefaultExternalSolverUri), Category("Sources"), DisplayName(@"External Solver URI"), Description("The feed URI used to get the external solver.")]
        public FeedUri ExternalSolverUri { get; set; } = new FeedUri(DefaultExternalSolverUri);

        /// <summary>
        /// The default value for <see cref="SyncServer"/>.
        /// </summary>
        public const string DefaultSyncServer = "https://0install.de/sync/";

        private Uri _syncServer = new Uri(DefaultSyncServer);

        /// <summary>
        /// The sync server used to synchronize your app list between multiple computers.
        /// </summary>
        /// <seealso cref="SyncServerUsername"/>
        /// <seealso cref="SyncServerPassword"/>
        [DefaultValue(typeof(Uri), DefaultSyncServer), Category("Sync"), DisplayName(@"Server"), Description("The sync server used to synchronize your app list between multiple computers.")]
        public Uri SyncServer { get => _syncServer; set => _syncServer = value?.ReparseAsAbsolute(); }

        /// <summary>
        /// The username to authenticate with against the <see cref="SyncServer"/>.
        /// </summary>
        /// <seealso cref="SyncServer"/>
        /// <seealso cref="SyncServerPassword"/>
        [DefaultValue(""), Category("Sync"), DisplayName(@"Username"), Description("The username to authenticate with against the Sync server.")]
        public string SyncServerUsername { get; set; } = "";

        /// <summary>
        /// The password to authenticate with against the <see cref="SyncServer"/>.
        /// </summary>
        /// <seealso cref="SyncServer"/>
        /// <seealso cref="SyncServerUsername"/>
        [DefaultValue(""), PasswordPropertyText(true), Category("Sync"), DisplayName(@"Password"), Description("The password to authenticate with against the Sync server.")]
        public string SyncServerPassword { get; set; } = "";

        /// <summary>
        /// The local key used to encrypt data before sending it to the <see cref="SyncServer"/>.
        /// </summary>
        [DefaultValue(""), PasswordPropertyText(true), Category("Sync"), DisplayName(@"Crypto key"), Description("The local key used to encrypt data before sending it to the Sync server.")]
        public string SyncCryptoKey { get; set; } = "";

        /// <summary>Provides meta-data for loading and saving settings properties.</summary>
        private readonly Dictionary<string, PropertyPointer<string>> _metaData;

        /// <summary>
        /// Creates a new configuration with default values set.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Key-value dispatcher")]
        public Config()
        {
            _metaData = new Dictionary<string, PropertyPointer<string>>
            {
                {"freshness", Property(() => Freshness, value => Freshness = value, defaultValue: _defaultFreshness).ToStringPointer()},
                {"help_with_testing", Property(() => HelpWithTesting, value => HelpWithTesting = value).ToStringPointer()},
                {"network_use", UseConverterProperty()},
                {"auto_approve_keys", Property(() => AutoApproveKeys, value => AutoApproveKeys = value, defaultValue: true).ToStringPointer()},
                {"feed_mirror", Property(() => FeedMirror, value => FeedMirror = value, defaultValue: new Uri(DefaultFeedMirror)).ToStringPointer()},
                {"key_info_server", Property(() => KeyInfoServer, value => KeyInfoServer = value, defaultValue: new Uri(DefaultKeyInfoServer)).ToStringPointer()},
                {"self_update_uri", Property(() => SelfUpdateUri, value => SelfUpdateUri = value, defaultValue: new FeedUri(DefaultSelfUpdateUri)).ToStringPointer()},
                {"external_solver_uri", Property(() => ExternalSolverUri, value => ExternalSolverUri = value, defaultValue: new FeedUri(DefaultExternalSolverUri)).ToStringPointer()},
                {"sync_server", Property(() => SyncServer, value => SyncServer = value, defaultValue: new Uri(DefaultSyncServer)).ToStringPointer()},
                {"sync_server_user", Property(() => SyncServerUsername, value => SyncServerUsername = value, defaultValue: "")},
                {"sync_server_pw", Property(() => SyncServerPassword, value => SyncServerPassword = value, defaultValue: "", needsEncoding: true)},
                {"sync_crypto_key", Property(() => SyncCryptoKey, value => SyncCryptoKey = value, defaultValue: "", needsEncoding: true)},
            };
        }

        private static PropertyPointer<T> Property<T>([NotNull] Func<T> getValue, [NotNull] Action<T> setValue, T defaultValue = default, bool needsEncoding = false)
            => new PropertyPointer<T>(getValue, setValue, defaultValue, needsEncoding);

        /// <summary>
        /// Creates a <see cref="string"/> pointer referencing <see cref="NetworkUse"/>. Uses hardcoded string lookup tables.
        /// </summary>
        private PropertyPointer<string> UseConverterProperty()
            => Property<string>(
                getValue: () =>
                {
                    switch (NetworkUse)
                    {
                        case NetworkLevel.Full:
                            return "full";
                        case NetworkLevel.Minimal:
                            return "minimal";
                        case NetworkLevel.Offline:
                            return "off-line";
                    }
                    return null; // Will never be reached
                },
                setValue: value =>
                {
                    switch (value)
                    {
                        case "full":
                            NetworkUse = NetworkLevel.Full;
                            return;
                        case "minimal":
                            NetworkUse = NetworkLevel.Minimal;
                            return;
                        case "off-line":
                            NetworkUse = NetworkLevel.Offline;
                            return;
                        default:
                            throw new FormatException("Must be 'full', 'minimal' or 'off-line'");
                    }
                },
                defaultValue: "full");
    }
}
