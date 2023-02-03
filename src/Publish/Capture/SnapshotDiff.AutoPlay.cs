// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Microsoft.Win32;
using NanoByte.Common.Native;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Publish.Capture;

partial class SnapshotDiff
{
    /// <summary>
    /// Collects data about AutoPlay handlers.
    /// </summary>
    /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
    /// <param name="capabilities">The capability list to add the collected data to.</param>
    /// <exception cref="IOException">There was an error accessing the registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
    public void CollectAutoPlays(CommandMapper commandMapper, CapabilityList capabilities)
    {
        #region Sanity checks
        if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
        if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
        #endregion

        capabilities.Entries.Add(AutoPlayHandlersUser
                                .Select(handler => GetAutoPlay(handler, Registry.CurrentUser, AutoPlayAssocsUser, commandMapper))
                                .WhereNotNull());

        capabilities.Entries.Add(AutoPlayHandlersMachine
                                .Select(handler => GetAutoPlay(handler, Registry.LocalMachine, AutoPlayAssocsMachine, commandMapper))
                                .WhereNotNull());
    }

    /// <summary>
    /// Retrieves data about a AutoPlay handler type from a snapshot diff.
    /// </summary>
    /// <param name="handler">The internal name of the AutoPlay handler.</param>
    /// <param name="hive">The registry hive to search in (usually HKCU or HKLM).</param>
    /// <param name="autoPlayAssocs">A list of associations of an AutoPlay events with an AutoPlay handlers</param>
    /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
    /// <exception cref="IOException">There was an error accessing the registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
    private static Capability? GetAutoPlay(string handler, RegistryKey hive, IEnumerable<(string name, string handler)> autoPlayAssocs, CommandMapper commandMapper)
    {
        #region Sanity checks
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (hive == null) throw new ArgumentNullException(nameof(hive));
        if (autoPlayAssocs == null) throw new ArgumentNullException(nameof(autoPlayAssocs));
        if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
        #endregion

        using var handlerKey = hive.TryOpenSubKey($@"{DesktopIntegration.Windows.AutoPlay.RegKeyHandlers}\{handler}");
        if (handlerKey?.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueProgID)?.ToString() is {} progID
         && handlerKey.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueVerb)?.ToString() is {} verbName
         && handlerKey.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueProvider)?.ToString() is {} provider)
        {
            using var progIDKey = Registry.ClassesRoot.TryOpenSubKey(progID) ?? throw new IOException($"{progID} key not found");
            return new AutoPlay
            {
                ID = handler,
                Provider = provider,
                Descriptions = {handlerKey.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueDescription)?.ToString() ?? throw new IOException("Missing description for AutoPlay handler.")},
                Verb = GetVerb(progIDKey, commandMapper, verbName) ?? throw new IOException($"Unable to find verb '{verbName}' for autoplay handler."),
                Events = {autoPlayAssocs.Where(x => x.handler == handler).Select(x => new AutoPlayEvent {Name = x.name})}
            };
        }

        return null;
    }
}
