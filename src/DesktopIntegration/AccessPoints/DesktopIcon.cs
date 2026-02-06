// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Creates an icon for an application on the user's desktop.
/// </summary>
[XmlType(TagName, Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class DesktopIcon : IconAccessPoint
{
    public const string TagName = "desktop-icon", AltName = "desktop";

    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry) => [$"{TagName}:{Name}"];

    /// <inheritdoc/>
    public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        ValidateName();

        var target = new FeedTarget(appEntry.InterfaceUri, feed);
        if (WindowsUtils.IsWindows) Windows.Shortcut.Create(this, target, iconStore, machineWide);
        else if (UnixUtils.IsMacOSX) MacOS.FreeDesktop.Create(this, target, iconStore, machineWide);
        else if (UnixUtils.IsUnix) Unix.FreeDesktop.Create(this, target, iconStore, machineWide);
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        ValidateName();

        if (WindowsUtils.IsWindows) Windows.Shortcut.Remove(this, machineWide);
        else if (UnixUtils.IsMacOSX) MacOS.FreeDesktop.Remove(this, machineWide);
        else if (UnixUtils.IsUnix) Unix.FreeDesktop.Remove(this, machineWide);
    }

    #region Clone
    /// <inheritdoc/>
    public override AccessPoint Clone() => new DesktopIcon {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command};
    #endregion
}
