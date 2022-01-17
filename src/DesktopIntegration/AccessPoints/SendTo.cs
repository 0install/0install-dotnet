// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Generator.Equals;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Creates a shortcut for an application in the "Send to" menu.
/// </summary>
[XmlType(TagName, Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class SendTo : IconAccessPoint
{
    public const string TagName = "send-to";

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
        if (WindowsUtils.IsWindows && !machineWide) Windows.Shortcut.Create(this, target, iconStore);
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        ValidateName();

        if (WindowsUtils.IsWindows && !machineWide) Windows.Shortcut.Remove(this);
    }

    #region Clone
    /// <inheritdoc/>
    public override AccessPoint Clone() => new SendTo {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command};
    #endregion
}
