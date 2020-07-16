// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Integrates an application into a file manager's context menu.
    /// </summary>
    /// <seealso cref="Model.Capabilities.ContextMenu"/>
    [XmlType("context-menu", Namespace = AppList.XmlNamespace)]
    public class ContextMenu : DefaultAccessPoint, IEquatable<ContextMenu>
    {
        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            var capability = appEntry.LookupCapability<Model.Capabilities.ContextMenu>(Capability);
            return new[] {$@"context-menu-{capability.Target}:{(capability.Verbs.Count == 1 ? capability.Verbs.Single().Name : capability.ID)}"};
        }

        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var capability = appEntry.LookupCapability<Model.Capabilities.ContextMenu>(Capability);
            var target = new FeedTarget(appEntry.InterfaceUri, feed);
            if (WindowsUtils.IsWindows) Windows.ContextMenu.Apply(target, capability, iconStore, machineWide);
            else if (UnixUtils.IsUnix) Unix.ContextMenu.Apply(target, capability, iconStore, machineWide);
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            var capability = appEntry.LookupCapability<Model.Capabilities.ContextMenu>(Capability);
            if (WindowsUtils.IsWindows) Windows.ContextMenu.Remove(capability, machineWide);
            else if (UnixUtils.IsUnix) Unix.ContextMenu.Remove(capability, machineWide);
        }

        #region Conversion
        /// <summary>
        /// Returns the access point in the form "ContextMenu". Not safe for parsing!
        /// </summary>
        public override string ToString() => "ContextMenu";
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint Clone() => new ContextMenu {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Capability = Capability};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ContextMenu other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(ContextMenu) && Equals((ContextMenu)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
