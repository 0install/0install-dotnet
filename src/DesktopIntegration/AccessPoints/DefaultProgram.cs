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
    /// Makes an application a default program of some kind (e.g. default web-browser, default e-mail client, ...).
    /// </summary>
    /// <seealso cref="Model.Capabilities.DefaultProgram"/>
    [XmlType("default-program", Namespace = AppList.XmlNamespace)]
    public class DefaultProgram : DefaultAccessPoint, IEquatable<DefaultProgram>
    {
        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            var capability = appEntry.LookupCapability<Model.Capabilities.DefaultProgram>(Capability);
            return new[] {$"clients:{capability.Service}"};
        }

        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var capability = appEntry.LookupCapability<Model.Capabilities.DefaultProgram>(Capability);
            var target = new FeedTarget(appEntry.InterfaceUri, feed);
            if (WindowsUtils.IsWindows && machineWide)
                Windows.DefaultProgram.Register(target, capability, iconStore, accessPoint: true);
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            var capability = appEntry.LookupCapability<Model.Capabilities.DefaultProgram>(Capability);
            if (WindowsUtils.IsWindows && machineWide)
                Windows.DefaultProgram.Unregister(capability, accessPoint: true);
        }

        #region Conversion
        /// <summary>
        /// Returns the access point in the form "DefaultProgram". Not safe for parsing!
        /// </summary>
        public override string ToString() => "DefaultProgram";
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint Clone() => new DefaultProgram {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Capability = Capability};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(DefaultProgram other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(DefaultProgram) && Equals((DefaultProgram)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
