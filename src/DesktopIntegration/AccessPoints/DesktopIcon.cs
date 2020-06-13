// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using NanoByte.Common.Native;
using ZeroInstall.Model;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Creates an icon for an application on the user's desktop.
    /// </summary>
    [XmlType("desktop-icon", Namespace = AppList.XmlNamespace)]
    public class DesktopIcon : IconAccessPoint, IEquatable<DesktopIcon>
    {
        #region Constants
        /// <summary>
        /// The name of this category of <see cref="AccessPoint"/>s as used by command-line interfaces.
        /// </summary>
        public const string CategoryName = "desktop";
        #endregion

        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry) => new[] {$"desktop:{Name}"};

        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var target = new FeedTarget(appEntry.InterfaceUri, feed);
            if (WindowsUtils.IsWindows) Windows.Shortcut.Create(this, target, iconStore, machineWide);
            else if (UnixUtils.IsUnix) Unix.FreeDesktop.Create(this, target, iconStore, machineWide);
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            if (WindowsUtils.IsWindows) Windows.Shortcut.Remove(this, machineWide);
            else if (UnixUtils.IsUnix) Unix.FreeDesktop.Remove(this, machineWide);
        }

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint Clone() => new DesktopIcon {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(DesktopIcon other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(DesktopIcon) && Equals((DesktopIcon)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
