// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Creates a shortcut for an application in the Quick Launch bar.
    /// </summary>
    [XmlType("quick-launch", Namespace = AppList.XmlNamespace)]
    public class QuickLaunch : IconAccessPoint, IEquatable<QuickLaunch>
    {
        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry) => new[] {$"quick-launch:{Name}"};

        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var target = new FeedTarget(appEntry.InterfaceUri, feed);
            if (WindowsUtils.IsWindows && !machineWide) Windows.Shortcut.Create(this, target, iconStore);
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            if (WindowsUtils.IsWindows && !machineWide) Windows.Shortcut.Remove(this);
        }

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint Clone() => new QuickLaunch {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(QuickLaunch? other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(QuickLaunch) && Equals((QuickLaunch)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
