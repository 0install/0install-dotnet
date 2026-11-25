// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Contains control logic for applying <see cref="BrowserNativeMessaging"/> on Linux and similar systems.
/// </summary>
public static class BrowserNativeMessagingHost
{
    /// <summary>
    /// Registers a browser native messaging host in the current system.
    /// </summary>
    /// <param name="target">The application being integrated.</param>
    /// <param name="capability">The native messaging host to register.</param>
    /// <param name="machineWide">Register the native messaging host machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Register(FeedTarget target, BrowserNativeMessaging capability, bool machineWide)
    {
        #region Sanity checks
        if (capability == null) throw new ArgumentNullException(nameof(capability));
        #endregion

        // TODO: Implement
    }

    /// <summary>
    /// Unregisters a browser native messaging host in the current system.
    /// </summary>
    /// <param name="capability">The native messaging host to remove.</param>
    /// <param name="machineWide">The native messaging host was registered machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Unregister(BrowserNativeMessaging capability, bool machineWide)
    {
        #region Sanity checks
        if (capability == null) throw new ArgumentNullException(nameof(capability));
        #endregion

        // TODO: Implement
    }
}
