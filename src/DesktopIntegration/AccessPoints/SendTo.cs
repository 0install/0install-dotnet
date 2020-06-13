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
    /// Creates a shortcut for an application in the "Send to" menu.
    /// </summary>
    [XmlType("send-to", Namespace = AppList.XmlNamespace)]
    public class SendTo : IconAccessPoint, IEquatable<SendTo>
    {
        #region Constants
        /// <summary>
        /// The name of this category of <see cref="AccessPoint"/>s as used by command-line interfaces.
        /// </summary>
        public const string CategoryName = "send-to";
        #endregion

        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry) => new[] {$"send-to:{Name}"};

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
        public override AccessPoint Clone() => new SendTo {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(SendTo other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(SendTo) && Equals((SendTo)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
