// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using NanoByte.Common;
using ZeroInstall.Model;

#if NETFRAMEWORK
using NanoByte.Common.Collections;
#endif

namespace ZeroInstall.Store
{
    /// <summary>
    /// User settings controlling network behaviour, solving, etc.
    /// </summary>
    [Serializable]
    public sealed partial class Config : IEnumerable<KeyValuePair<string, string>>, ICloneable<Config>, IEquatable<Config>
    {
        /// <summary>
        /// The initial tab to show in GUI representations.
        /// </summary>
        public ConfigTab InitialTab { get; set; }

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
            get => _networkLevel;
            set
            {
                if (!Enum.IsDefined(typeof(NetworkLevel), value)) throw new ArgumentOutOfRangeException(nameof(value));
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

        /// <summary>
        /// The mirror server used to provide feeds when the original server is unavailable. Set to empty to deactivate use of feed mirror.
        /// </summary>
        [DefaultValue(typeof(FeedUri), DefaultFeedMirror), Category("Sources"), DisplayName(@"Feed mirror"), Description("The mirror server used to provide feeds when the original server is unavailable. Set to empty to deactive use of feed mirror.")]
        public FeedUri? FeedMirror { get; set; } = new(DefaultFeedMirror);

        /// <summary>
        /// The default value for <see cref="KeyInfoServer"/>.
        /// </summary>
        public const string DefaultKeyInfoServer = "https://keylookup.0install.net/";

        /// <summary>
        /// The key information server used to get information about who signed a feed. Set to empty to deactive use of key information server.
        /// </summary>
        [DefaultValue(typeof(FeedUri), DefaultKeyInfoServer), Category("Sources"), DisplayName(@"Key info server"), Description("The key information server used to get information about who signed a feed. Set to empty to deactive use of key information server.")]
        public FeedUri? KeyInfoServer { get; set; } = new(DefaultKeyInfoServer);

        /// <summary>
        /// The default value for <see cref="SelfUpdateUri"/>.
        /// </summary>
        public const string DefaultSelfUpdateUri
#if NETFRAMEWORK
            = "https://apps.0install.net/0install/0install-win.xml";
#else
            = "https://apps.0install.net/0install/0install-dotnet.xml";
#endif

        /// <summary>
        /// The feed URI used to search for updates for Zero Install itself. Set to empty to deactive self-update.
        /// </summary>
        [DefaultValue(typeof(FeedUri), DefaultSelfUpdateUri), Category("Sources"), DisplayName(@"Self-update URI"), Description("The feed URI used by the solver to search for updates for Zero Install itself. Set to empty to deactive self-update.")]
        public FeedUri? SelfUpdateUri { get; set; } = new(DefaultSelfUpdateUri);

        /// <summary>
        /// The default value for <see cref="ExternalSolverUri"/>.
        /// </summary>
        public const string DefaultExternalSolverUri = "http://0install.net/tools/0install.xml";

        /// <summary>
        /// The feed URI used to get the external solver. Set to empty to deactive use of external solver.
        /// </summary>
        [DefaultValue(typeof(FeedUri), DefaultExternalSolverUri), Category("Sources"), DisplayName(@"External Solver URI"), Description("The feed URI used to get the external solver. Set to empty to deactive use of external solver.")]
        public FeedUri? ExternalSolverUri { get; set; } = new(DefaultExternalSolverUri);

        /// <summary>
        /// The default value for <see cref="SyncServer"/>.
        /// </summary>
        public const string DefaultSyncServer = "https://0install.de/sync/";

        /// <summary>
        /// The sync server used to synchronize your app list between multiple computers.
        /// </summary>
        /// <seealso cref="SyncServerUsername"/>
        /// <seealso cref="SyncServerPassword"/>
        [DefaultValue(typeof(FeedUri), DefaultSyncServer), Category("Sync"), DisplayName(@"Server"), Description("The sync server used to synchronize your app list between multiple computers.")]
        public FeedUri? SyncServer { get; set; } = new(DefaultSyncServer);

        /// <summary>
        /// The username to authenticate with against the <see cref="SyncServer"/>.
        /// </summary>
        /// <seealso cref="SyncServer"/>
        /// <seealso cref="SyncServerPassword"/>
        [DefaultValue(""), Category("Sync"), DisplayName(@"Username"), Description("The username to authenticate with against the sync server.")]
        public string SyncServerUsername { get; set; } = "";

        /// <summary>
        /// The password to authenticate with against the <see cref="SyncServer"/>.
        /// </summary>
        /// <seealso cref="SyncServer"/>
        /// <seealso cref="SyncServerUsername"/>
        [DefaultValue(""), PasswordPropertyText(true), Category("Sync"), DisplayName(@"Password"), Description("The password to authenticate with against the sync server.")]
        public string SyncServerPassword { get; set; } = "";

        /// <summary>
        /// The local key used to encrypt data before sending it to the <see cref="SyncServer"/>.
        /// </summary>
        [DefaultValue(""), PasswordPropertyText(true), Category("Sync"), DisplayName(@"Crypto key"), Description("The local key used to encrypt data before sending it to the sync server.")]
        public string SyncCryptoKey { get; set; } = "";

        /// <summary>
        /// Indicates whether the sync-related configuration is complete.
        /// </summary>
        [Browsable(false)]
        public bool IsSyncConfigured
            => SyncServer != null
            && (SyncServer.IsFile || (!string.IsNullOrEmpty(SyncServerUsername) && !string.IsNullOrEmpty(SyncServerPassword) && !string.IsNullOrEmpty(SyncCryptoKey)));

        /// <summary>Provides meta-data for loading and saving settings properties.</summary>
        private readonly Dictionary<string, PropertyPointer<string>> _metaData;

        /// <summary>
        /// Creates a new configuration with default values set.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Key-value dispatcher")]
        public Config()
        {
            _metaData = new()
            {
                {"freshness", PropertyPointer.For(() => Freshness, defaultValue: _defaultFreshness).ToStringPointer()},
                {"help_with_testing", PropertyPointer.For(() => HelpWithTesting, value => HelpWithTesting = value, defaultValue: false).ToStringPointer()},
                {"network_use", NetworkUsePropertyPointer},
                {"auto_approve_keys", PropertyPointer.For(() => AutoApproveKeys, defaultValue: true).ToStringPointer()},
                {"feed_mirror", FeedUriPropertyPointer(() => FeedMirror, DefaultFeedMirror)},
                {"key_info_server", FeedUriPropertyPointer(() => KeyInfoServer, DefaultKeyInfoServer)},
                {"self_update_uri", FeedUriPropertyPointer(() => SelfUpdateUri, DefaultSelfUpdateUri)},
                {"external_solver_uri", FeedUriPropertyPointer(() => ExternalSolverUri, DefaultExternalSolverUri)},
                {"sync_server", FeedUriPropertyPointer(() => SyncServer, DefaultSyncServer)},
                {"sync_server_user", PropertyPointer.For(() => SyncServerUsername, defaultValue: "")},
                {"sync_server_pw", PropertyPointer.For(() => SyncServerPassword, defaultValue: "", needsEncoding: true)},
                {"sync_crypto_key", PropertyPointer.For(() => SyncCryptoKey, defaultValue: "", needsEncoding: true)},
            };
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach ((string key, var property) in _metaData)
                yield return new KeyValuePair<string, string>(key, property.NeedsEncoding ? "***" : property.Value);
        }

        /// <summary>
        /// Wraps a <see cref="FeedUri"/> pointer in a <see cref="string"/> pointer. Maps empty strings to <c>null</c> URIs.
        /// </summary>
        private static PropertyPointer<string> FeedUriPropertyPointer(Expression<Func<FeedUri?>> expression, string defaultValue)
        {
            var property = PropertyPointer.For(expression, null);
            return PropertyPointer.For(
                () => property.Value?.ToStringRfc() ?? "",
                value => property.Value = string.IsNullOrEmpty(value) ? default : new FeedUri(value),
                defaultValue);
        }

        /// <summary>
        /// Creates a <see cref="string"/> pointer referencing <see cref="NetworkUse"/>. Uses hardcoded string lookup tables.
        /// </summary>
        private PropertyPointer<string> NetworkUsePropertyPointer
            => PropertyPointer.For(
                getValue: () => NetworkUse switch
                {
                    NetworkLevel.Full => "full",
                    NetworkLevel.Minimal => "minimal",
                    NetworkLevel.Offline => "off-line",
                    _ => throw new InvalidEnumArgumentException() // Will never be reached
                },
                setValue: value =>
                {
                    NetworkUse = value switch
                    {
                        "full" => NetworkLevel.Full,
                        "minimal" => NetworkLevel.Minimal,
                        "off-line" => NetworkLevel.Offline,
                        _ => throw new FormatException("Must be 'full', 'minimal' or 'off-line'")
                    };
                },
                defaultValue: "full");
    }
}
