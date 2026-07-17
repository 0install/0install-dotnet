// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Client;

/// <summary>
/// Well-known access point categories suitable for passing as <c>add</c> and <c>remove</c> parameters to <see cref="IZeroInstallClient.IntegrateAsync"/>.
/// </summary>
public static class IntegrationCategories
{
    /// <summary>
    /// Registers all compatible capabilities (file types, URL protocols, etc.) the application provides with the system.
    /// </summary>
    public const string CapabilityRegistration = "capability-registration";

    /// <summary>
    /// Creates an entry for the application in the start menu or application launcher.
    /// </summary>
    public const string MenuEntry = "menu-entry";

    /// <summary>
    /// Creates a shortcut for the application on the desktop.
    /// </summary>
    public const string DesktopIcon = "desktop-icon";

    /// <summary>
    /// Adds the application to the "Send to" context menu.
    /// </summary>
    public const string SendTo = "send-to";

    /// <summary>
    /// Makes the application discoverable from the command-line via the system's search PATH.
    /// </summary>
    public const string Alias = "alias";

    /// <summary>
    /// Automatically starts the application when the user logs in.
    /// </summary>
    public const string AutoStart = "auto-start";

    /// <summary>
    /// Makes the application the default handler for the capabilities it provides (file types, URL protocols, etc.).
    /// </summary>
    public const string DefaultAccessPoint = "default-access-point";

    /// <summary>
    /// All known access point categories.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [CapabilityRegistration, MenuEntry, DesktopIcon, SendTo, Alias, AutoStart, DefaultAccessPoint];

    /// <summary>
    /// The recommended standard access point categories.
    /// </summary>
    public static IReadOnlyList<string> Standard { get; } = [CapabilityRegistration, MenuEntry, SendTo, Alias];
}
