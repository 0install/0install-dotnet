// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections;

namespace ZeroInstall.Store.Configuration;

/// <summary>
/// User settings controlling network behaviour, solving, etc..
/// </summary>
[Serializable]
public sealed partial class Config : IEnumerable<KeyValuePair<string, string>>, ICloneable<Config>, IEquatable<Config>
{
    /// <summary>
    /// The initial tab to show in GUI representations.
    /// </summary>
    [Browsable(false)]
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

    private NetworkLevel _networkUse = NetworkLevel.Full;

    /// <summary>
    /// Controls how liberally network access is attempted.
    /// </summary>
    [DefaultValue(typeof(NetworkLevel), "Full"), Category("Policy"), DisplayName(@"Network use"), Description("Controls how liberally network access is attempted.")]
    public NetworkLevel NetworkUse
    {
        get => _networkUse;
        set
        {
            if (!Enum.IsDefined(typeof(NetworkLevel), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _networkUse = value;
        }
    }

    /// <summary>
    /// Automatically approve keys known by the <see cref="KeyInfoServer"/> and seen the first time a feed is fetched.
    /// </summary>
    [DefaultValue(true), Category("Policy"), DisplayName(@"Auto approve keys"), Description("Automatically approve keys known by the key info server and seen the first time a feed is fetched.")]
    public bool AutoApproveKeys { get; set; } = true;

    /// <summary>
    /// The default value for <see cref="MaxParallelDownloads"/>.
    /// </summary>
    public const int DefaultMaxParallelDownloads = 4;

    /// <summary>
    /// Maximum number of <see cref="Implementation"/>s to download in parallel.
    /// </summary>
    [DefaultValue(DefaultMaxParallelDownloads), Category("Fetcher"), DisplayName(@"Maximum parallel downloads"), Description("Maximum number of implementations to download in parallel.")]
    public int MaxParallelDownloads { get; set; } = DefaultMaxParallelDownloads;

    /// <summary>
    /// The default value for <see cref="FeedMirror"/>.
    /// </summary>
    public const string DefaultFeedMirror = "https://roscidus.com/0mirror";

    /// <summary>
    /// The mirror server used to provide feeds when the original server is unavailable. Set to empty to deactivate use of feed mirror.
    /// </summary>
    [DefaultValue(typeof(FeedUri), DefaultFeedMirror), Category("Sources"), DisplayName(@"Feed mirror"), Description("The mirror server used to provide feeds when the original server is unavailable. Set to empty to deactivate use of feed mirror.")]
    public FeedUri? FeedMirror { get; set; } = new(DefaultFeedMirror);

    /// <summary>
    /// The default value for <see cref="KeyInfoServer"/>.
    /// </summary>
    public const string DefaultKeyInfoServer = "https://keylookup.0install.net/";

    /// <summary>
    /// The key information server used to get information about who signed a feed. Set to empty to deactivate use of key information server.
    /// </summary>
    [DefaultValue(typeof(FeedUri), DefaultKeyInfoServer), Category("Sources"), DisplayName(@"Key info server"), Description("The key information server used to get information about who signed a feed. Set to empty to deactivate use of key information server.")]
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
    /// The feed URI used to search for updates for Zero Install itself. Set to empty to deactivate self-update.
    /// </summary>
    [DefaultValue(typeof(FeedUri), DefaultSelfUpdateUri), Category("Sources"), DisplayName(@"Self-update URI"), Description("The feed URI used by the solver to search for updates for Zero Install itself. Set to empty to deactivate self-update.")]
    public FeedUri? SelfUpdateUri { get; set; } = new(DefaultSelfUpdateUri);

    /// <summary>
    /// The default value for <see cref="ExternalSolverUri"/>.
    /// </summary>
    public const string DefaultExternalSolverUri = "https://apps.0install.net/0install/0install-ocaml.xml";

    /// <summary>
    /// The feed URI used to get the external solver. Set to empty to deactivate use of external solver.
    /// </summary>
    [DefaultValue(typeof(FeedUri), DefaultExternalSolverUri), Category("Sources"), DisplayName(@"External Solver URI"), Description("The feed URI used to get the external solver. Set to empty to deactivate use of external solver.")]
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
    [MemberNotNullWhen(true, nameof(SyncServer))]
    public bool IsSyncConfigured
        => SyncServer != null
        && (SyncServer.IsFile || (!string.IsNullOrEmpty(SyncServerUsername) && !string.IsNullOrEmpty(SyncServerPassword) && !string.IsNullOrEmpty(SyncCryptoKey)));

    /// <summary>Provides meta-data for loading and saving settings properties.</summary>
    private readonly Dictionary<string, ConfigProperty> _metaData;

    /// <summary>
    /// Creates a new configuration with default values set.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Key-value dispatcher")]
    public Config()
    {
        _metaData = new()
        {
            ["freshness"] = ConfigProperty.For(() => Freshness, _defaultFreshness),
            ["help_with_testing"] = ConfigProperty.For(() => HelpWithTesting),
            ["network_use"] = ConfigProperty.For(() => NetworkUse, defaultValue: NetworkLevel.Full),
            ["auto_approve_keys"] = ConfigProperty.For(() => AutoApproveKeys, defaultValue: true),
            ["max_parallel_downloads"] = ConfigProperty.For(() => MaxParallelDownloads, DefaultMaxParallelDownloads),
            ["feed_mirror"] = ConfigProperty.For(() => FeedMirror, DefaultFeedMirror),
            ["key_info_server"] = ConfigProperty.For(() => KeyInfoServer, DefaultKeyInfoServer),
            ["self_update_uri"] = ConfigProperty.For(() => SelfUpdateUri, DefaultSelfUpdateUri),
            ["external_solver_uri"] = ConfigProperty.For(() => ExternalSolverUri, DefaultExternalSolverUri),
            ["sync_server"] = ConfigProperty.For(() => SyncServer, DefaultSyncServer),
            ["sync_server_user"] = ConfigProperty.For(() => SyncServerUsername, defaultValue: ""),
            ["sync_server_pw"] = ConfigProperty.For(() => SyncServerPassword, defaultValue: "", needsEncoding: true),
            ["sync_crypto_key"] = ConfigProperty.For(() => SyncCryptoKey, defaultValue: "", needsEncoding: true),
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
}
