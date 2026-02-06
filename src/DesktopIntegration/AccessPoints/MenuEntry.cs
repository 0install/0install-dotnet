// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Creates an entry for an application in the user's application menu (i.e. Windows start menu, GNOME application menu, etc.).
/// </summary>
[XmlType(TagName, Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class MenuEntry : IconAccessPoint
{
    public const string TagName = "menu-entry", AltName = "menu";

    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry) => [$"{TagName}:{Category}/{Name}"];

    /// <summary>
    /// The category or folder in the menu to add the entry to. Leave empty for top-level entry.
    /// </summary>
    [Description("The category or folder in the menu to add the entry to. Leave empty for top-level entry.")]
    [XmlAttribute("category")]
    public string? Category { get; set; }

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
    public override AccessPoint Clone() => new MenuEntry {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command, Category = Category};
    #endregion
}
