// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common.Native;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Indicates that all compatible capabilities should be registered.
    /// </summary>
    /// <seealso cref="ZeroInstall.Store.Model.Capabilities"/>
    [XmlType("capability-registration", Namespace = AppList.XmlNamespace)]
    public class CapabilityRegistration : AccessPoint, IEquatable<CapabilityRegistration>
    {
        #region Constants
        /// <summary>
        /// The name of this category of <see cref="AccessPoint"/>s as used by command-line interfaces.
        /// </summary>
        public const string CategoryName = "capabilities";
        #endregion

        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            return appEntry.CapabilityLists.CompatibleCapabilities().SelectMany(x => x.ConflictIDs);
        }

        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var capabilities = appEntry.CapabilityLists.CompatibleCapabilities().ToList();
            var target = new FeedTarget(appEntry.InterfaceUri, feed);

            foreach (var capability in capabilities)
            {
                switch (capability)
                {
                    case Store.Model.Capabilities.FileType fileType:
                        if (WindowsUtils.IsWindows) Windows.FileType.Register(target, fileType, iconStore, machineWide);
                        else if (UnixUtils.IsUnix) Unix.FileType.Register(target, fileType, iconStore, machineWide);
                        break;

                    case Store.Model.Capabilities.UrlProtocol urlProtocol:
                        if (WindowsUtils.IsWindows) Windows.UrlProtocol.Register(target, urlProtocol, iconStore, machineWide);
                        else if (UnixUtils.IsUnix) Unix.UrlProtocol.Register(target, urlProtocol, iconStore, machineWide);
                        break;

                    case Store.Model.Capabilities.AutoPlay autoPlay:
                        if (WindowsUtils.IsWindows) Windows.AutoPlay.Register(target, autoPlay, iconStore, machineWide);
                        break;

                    case AppRegistration appRegistration:
                        if ((WindowsUtils.IsWindows && machineWide) || WindowsUtils.IsWindows8) Windows.AppRegistration.Register(target, appRegistration, capabilities.OfType<VerbCapability>(), iconStore, machineWide);
                        break;

                    case Store.Model.Capabilities.DefaultProgram defaultProgram:
                        if (WindowsUtils.IsWindows && machineWide) Windows.DefaultProgram.Register(target, defaultProgram, iconStore);
                        else if (UnixUtils.IsUnix) Unix.DefaultProgram.Register(target, defaultProgram, iconStore, machineWide);
                        break;

                    case ComServer comServer:
                        if (WindowsUtils.IsWindows) Windows.ComServer.Register(target, comServer, iconStore, machineWide);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            foreach (var capability in appEntry.CapabilityLists.CompatibleCapabilities())
            {
                switch (capability)
                {
                    case Store.Model.Capabilities.FileType fileType:
                        if (WindowsUtils.IsWindows) Windows.FileType.Unregister(fileType, machineWide);
                        else if (UnixUtils.IsUnix) Unix.FileType.Unregister(fileType, machineWide);
                        break;

                    case Store.Model.Capabilities.UrlProtocol urlProtocol:
                        if (WindowsUtils.IsWindows) Windows.UrlProtocol.Unregister(urlProtocol, machineWide);
                        else if (UnixUtils.IsUnix) Unix.UrlProtocol.Unregister(urlProtocol, machineWide);
                        break;

                    case Store.Model.Capabilities.AutoPlay autoPlay:
                        if (WindowsUtils.IsWindows) Windows.AutoPlay.Unregister(autoPlay, machineWide);
                        break;

                    case AppRegistration appRegistration:
                        if ((WindowsUtils.IsWindows && machineWide) || WindowsUtils.IsWindows8) Windows.AppRegistration.Unregister(appRegistration, machineWide);
                        break;

                    case Store.Model.Capabilities.DefaultProgram defaultProgram:
                        if (WindowsUtils.IsWindows && machineWide) Windows.DefaultProgram.Unregister(defaultProgram);
                        else if (UnixUtils.IsUnix) Unix.DefaultProgram.Unregister(defaultProgram, machineWide);
                        break;

                    case ComServer comServer:
                        if (WindowsUtils.IsWindows) Windows.ComServer.Unregister(comServer, machineWide);
                        break;
                }
            }
        }

        #region Conversion
        /// <summary>
        /// Returns the access point in the form "CapabilityRegistration". Not safe for parsing!
        /// </summary>
        public override string ToString() => "CapabilityRegistration";
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint Clone() => new CapabilityRegistration {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(CapabilityRegistration other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(CapabilityRegistration) && Equals((CapabilityRegistration)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
