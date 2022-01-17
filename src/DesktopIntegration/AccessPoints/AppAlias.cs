// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Makes an application discoverable via the system's search PATH.
/// </summary>
[XmlType(TagName, Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class AppAlias : CommandAccessPoint
{
    public const string TagName = "alias", AltName = "aliases";

    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry) => new[] { $"{TagName}:{Name}" };

    /// <inheritdoc/>
    public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        ValidateName();

        var target = new FeedTarget(appEntry.InterfaceUri, feed);
        if (WindowsUtils.IsWindows) Windows.AppAlias.Create(target, Command, Name, iconStore, machineWide);
        else if (UnixUtils.IsUnix) Unix.AppAlias.Create(target, Command, Name, iconStore, machineWide);
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        ValidateName();

        if (WindowsUtils.IsWindows) Windows.AppAlias.Remove(Name, machineWide);
        else if (UnixUtils.IsUnix) Unix.AppAlias.Remove(Name, machineWide);
    }

    #region Clone
    /// <inheritdoc/>
    public override AccessPoint Clone() => new AppAlias {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command};
    #endregion
}
