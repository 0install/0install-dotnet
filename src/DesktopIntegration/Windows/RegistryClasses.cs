// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Helpers for registering <see cref="Capability"/>s in the HKCR section of the Windows Registry.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class RegistryClasses
{
    /// <summary>
    /// Prepended before any programmatic identifiers used by Zero Install in the registry. This prevents conflicts with non-Zero Install installations.
    /// </summary>
    public const string Prefix = "ZeroInstall.";

    /// <summary>
    /// Prepended before any registry purpose flags. Purpose flags indicate what a registry key was created for and whether it is still required.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flag")]
    public const string PurposeFlagPrefix = "ZeroInstall.";

    /// <summary>
    /// Indicates a registry key is required by a capability.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flag")]
    public const string PurposeFlagCapability = PurposeFlagPrefix + "Capability";

    /// <summary>
    /// Indicates a registry key is required by an access point.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flag")]
    public const string PurposeFlagAccessPoint = PurposeFlagPrefix + "AccessPoint";

    /// <summary>
    /// Opens the HKCU/HKLM registry key backing HKCR.
    /// </summary>
    public static RegistryKey OpenHive(bool machineWide)
        => (machineWide ? Registry.LocalMachine : Registry.CurrentUser).OpenSubKeyChecked(@"SOFTWARE\Classes", writable: true);

    /// <summary>
    /// Registers a <see cref="VerbCapability"/> in a registry key.
    /// </summary>
    /// <param name="registryKey">The registry key to write the new data to.</param>
    /// <param name="target">The application being integrated.</param>
    /// <param name="capability">The capability to register.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="machineWide">Assume <paramref name="registryKey"/> is effective machine-wide instead of just for the current user.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public static void Register(RegistryKey registryKey, FeedTarget target, VerbCapability capability, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (capability == null) throw new ArgumentNullException(nameof(capability));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        if ((capability.GetIcon(Icon.MimeTypeIco) ?? target.Feed.Icons.GetIcon(Icon.MimeTypeIco)) is {} icon)
        {
            using var iconKey = registryKey.CreateSubKeyChecked("DefaultIcon");
            iconKey.SetValue("", $"{iconStore.GetFresh(icon)},0");
        }

        foreach (var verb in capability.Verbs)
        {
            using var verbKey = registryKey.CreateSubKeyChecked($@"shell\{verb.Name}");
            Register(verbKey, target, verb, iconStore, machineWide);
        }

        // Prevent conflicts with existing entries
        registryKey.TryDeleteSubKey(@"shell\ddeexec");
    }

    /// <summary>
    /// Registers a <see cref="Verb"/> in a registry key.
    /// </summary>
    /// <param name="verbKey">The registry key to write the new data to.</param>
    /// <param name="target">The application being integrated.</param>
    /// <param name="verb">The verb to register.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="machineWide">Assume <paramref name="verbKey"/> is effective machine-wide instead of just for the current user.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public static void Register(RegistryKey verbKey, FeedTarget target, Verb verb, IIconStore iconStore, bool machineWide)
    {
        string? description = verb.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture);
        verbKey.SetOrDelete("", description);
        verbKey.SetOrDelete("MUIVerb", description);

        verbKey.SetOrDelete("MultiSelectModel", verb.SingleElementOnly ? "Single" : null);

        if (verb.Extended) verbKey.SetValue("Extended", "");
        else verbKey.DeleteValue("Extended", throwOnMissingValue: false);

        var icon = target.Feed.GetBestIcon(Icon.MimeTypeIco, verb.Command)
                ?? target.Feed.Icons.GetIcon(Icon.MimeTypeIco);
        verbKey.SetOrDelete("Icon", icon?.To(iconStore.GetFresh));

        using var commandKey = verbKey.CreateSubKeyChecked("command");
        commandKey.SetValue("", GetLaunchCommandLine(target, verb, iconStore, machineWide));
    }

    /// <summary>
    /// Generates a command-line string for launching a <see cref="Verb"/>.
    /// </summary>
    /// <param name="target">The application being integrated.</param>
    /// <param name="verb">The verb to get to launch command for.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="machineWide">Store the stub in a machine-wide directory instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="InvalidOperationException">Write access to the filesystem is not permitted.</exception>
    internal static string GetLaunchCommandLine(FeedTarget target, Verb verb, IIconStore iconStore, bool machineWide)
    {
        IEnumerable<string> GetCommandLine()
        {
            try
            {
                return new StubBuilder(iconStore).GetRunCommandLine(target, verb.Command, machineWide);
            }
            #region Error handling
            catch (InvalidOperationException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        }

        if (verb.Arguments is [])
            return GetCommandLine().JoinEscapeArguments() + " " + (verb.ArgumentsLiteral.EmptyAsNull() ?? "\"%V\"");

        return GetCommandLine()
              .Concat(verb.Arguments.Select(x => x.Value))
              .JoinEscapeArguments()
              .Replace("${item}", "\"%V\"");
    }

    /// <summary>
    /// Sets <paramref name="name"/> to <paramref name="value"/> or deletes the entry if <paramref name="value"/> is <c>null</c> or empty.
    /// </summary>
    public static void SetOrDelete(this RegistryKey registryKey, string name, string? value)
    {
        if (string.IsNullOrEmpty(value)) registryKey.DeleteValue(name, throwOnMissingValue: false);
        else registryKey.SetValue(name, value);
    }
}
