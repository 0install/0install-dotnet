// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common.Native;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Makes an application the default AutoPlay handler for a specific event.
    /// </summary>
    /// <seealso cref="ZeroInstall.Store.Model.Capabilities.AutoPlay"/>
    [XmlType("auto-play", Namespace = AppList.XmlNamespace)]
    public class AutoPlay : DefaultAccessPoint, IEquatable<AutoPlay>
    {
        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            var capability = appEntry.LookupCapability<Store.Model.Capabilities.AutoPlay>(Capability);
            return capability.Events.Select(@event => $"autoplay-event:{@event.Name}");
        }

        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var capability = appEntry.LookupCapability<Store.Model.Capabilities.AutoPlay>(Capability);
            var target = new FeedTarget(appEntry.InterfaceUri, feed);
            if (WindowsUtils.IsWindows) Windows.AutoPlay.Register(target, capability, iconStore, machineWide, accessPoint: true);
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            var capability = appEntry.LookupCapability<Store.Model.Capabilities.AutoPlay>(Capability);
            if (WindowsUtils.IsWindows) Windows.AutoPlay.Unregister(capability, machineWide, accessPoint: true);
        }

        #region Conversion
        /// <summary>
        /// Returns the access point in the form "AutoPlay". Not safe for parsing!
        /// </summary>
        public override string ToString() => "AutoPlay";
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint Clone() => new AutoPlay {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Capability = Capability};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AutoPlay other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(AutoPlay) && Equals((AutoPlay)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
