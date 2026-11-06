// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using IniParser.Model;
using NanoByte.Common.Native;
using ZeroInstall.Model.Trust;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Bootstrap;

partial class BootstrapProcess
{
    /// <summary>
    /// The feed URI used to download Zero Install itself.
    /// </summary>
    private FeedUri SelfUpdateUri => Config.SelfUpdateUri ?? new(Config.DefaultSelfUpdateUri);

    /// <summary>
    /// Applies bundled defaults and command-line options to the configuration for this process.
    /// </summary>
    private void LoadConfig()
    {
        if (ApplyBootstrapConfig)
        {
            Config.ReadFrom(BootstrapConfig.Instance.IniData);
            Config.ReadFromGroupPolicy(); // Policies take precedence over the bootstrap config
        }
        else Log.Info("Ignoring bootstrap config due to existing Zero Install deployment");

        if (_selfUpdateUri != null) Config.SelfUpdateUri = _selfUpdateUri;
        if (_refresh) FeedManager.Refresh = true;
    }

    /// <summary>
    /// Persists options from the <see cref="BootstrapConfig"/> and <see cref="_selfUpdateUri"/> in the config file for the current scope if applicable.
    /// </summary>
    private void SaveConfig()
    {
        if (!ApplyBootstrapConfig) return;

        SaveBootstrapConfig(BootstrapConfig.Instance.IniData, _selfUpdateUri, _machineWide);

        if (BootstrapConfig.Instance.CatalogUri is {} catalogUri)
        {
            Log.Info($"Setting custom catalog source: {catalogUri}");
            Services.Feeds.CatalogManager.SetSources([catalogUri], _machineWide);
        }
    }

    /// <summary>
    /// Merges options from the <see cref="BootstrapConfig"/> into a single config file, keeping options from other sources out of it.
    /// </summary>
    /// <param name="iniData">The options from the <see cref="BootstrapConfig"/>.</param>
    /// <param name="selfUpdateUri">An alternative feed URI for Zero Install that takes precedence over <paramref name="iniData"/>.</param>
    /// <param name="machineWide"><c>true</c> to use the machine-wide config file; <c>false</c> to use the user-specific config file.</param>
    internal static void SaveBootstrapConfig(IniData iniData, FeedUri? selfUpdateUri, bool machineWide)
    {
        var config = new Config();
        if (machineWide) config.ReadFromFilesMachineWideOnly();
        else config.ReadFromFilesUserOnly();
        config.ReadFrom(iniData);
        if (selfUpdateUri != null) config.SelfUpdateUri = selfUpdateUri;
        config.Save(machineWide);
    }

    /// <summary>
    /// Indicates whether to apply options bundled together with the <see cref="BootstrapConfig"/>,
    /// potentially overwriting existing configuration.
    /// </summary>
    private bool ApplyBootstrapConfig
        => ZeroInstallDeployment.FindOther(_machineWide) is not {} deployedInstance // No existing deployment...
        || IsLibraryMode(deployedInstance); // ... or existing deployment is in library-mode

    /// <summary>
    /// Indicates whether a deployment found by <see cref="ZeroInstallDeployment.FindOther"/> was made in library mode.
    /// </summary>
    private bool IsLibraryMode(string deployedInstance)
        => ZeroInstallDeployment.IsLibraryMode(
            machineWide: _machineWide || deployedInstance != ZeroInstallDeployment.GetPath(machineWide: false));

    /// <summary>
    /// Asks the user to provide a custom path for storing implementations.
    /// </summary>
    private void CustomizeStorePath()
    {
        if (!TryGetCurrentStorePath(out string? currentPath)) return;

        string? newPath = _storePath ?? _handler.GetCustomStorePath(_machineWide, currentPath);

        if (newPath != null && IsMachineWidePath(newPath))
        {
            if (!OSUtils.IsAdmin) throw new NotAdminException();
            _machineWide = true;
        }

        SetStorePath(newPath);
    }

    private bool TryGetCurrentStorePath(out string? path)
    {
        var paths = (_machineWide
            ? ImplementationStores.GetMachineWideDirectories()
            : ImplementationStores.GetUserDirectories()).ToList();

        if (paths.Count > 1)
        {
            Log.Info("Refusing to customize path, because multiple custom implementation directories are already configured");
            path = null;
            return false;
        }

        path = paths.FirstOrDefault();
        return true;
    }

    private void SetStorePath(string? path)
    {
        string[] paths = string.IsNullOrEmpty(path) ? [] : [path];
        if (_machineWide) ImplementationStores.SetMachineWideDirectories(paths);
        else ImplementationStores.SetUserDirectories(paths);
    }

    private static bool IsMachineWidePath(string path)
        => WindowsUtils.IsWindows
        && (path.StartsWithIgnoreCase(WindowsUtils.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
         || path.StartsWithIgnoreCase(WindowsUtils.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));

    /// <summary>
    /// Adds keys for Zero Install (and optionally an app) to the <see cref="TrustDB"/>.
    /// </summary>
    private void TrustKeys()
    {
        try
        {
            var trust = _machineWide
                ? TrustDB.LoadMachineWide()
                : TrustDB.Load();
            trust.TrustKey("88C8A1F375928691D7365C0259AA3927C24E4E1E", new("apps.0install.net"));
            if (BootstrapConfig.Instance.KeyFingerprint is {} fingerprint)
            {
                if (_appUri != null)
                    trust.TrustKey(fingerprint, new(_appUri.Host));
                else if (Config.SelfUpdateUri is {} selfUpdateUri)
                    trust.TrustKey(fingerprint, new(selfUpdateUri.Host));
            }
            trust.Save();
        }
        #region Error handling
        catch (Exception ex)
        {
            Log.Error("Failed to add keys to trust database", ex);
        }
        #endregion
    }
}
