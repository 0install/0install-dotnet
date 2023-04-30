// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Controls how synchronization data is reset by <see cref="SyncIntegrationManager.Sync"/>
/// </summary>
public enum SyncResetMode
{
    /// <summary>Merge data from client and server normally.</summary>
    None,

    /// <summary>Replace all data on the client with data from the server.</summary>
    Client,

    /// <summary>Replace all data on the server with data from the client.</summary>
    Server
}
