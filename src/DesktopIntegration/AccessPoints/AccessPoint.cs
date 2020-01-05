// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using NanoByte.Common;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// An access point represents changes to the desktop environment's UI which the user explicitly requested.
    /// </summary>
    [XmlType("access-point", Namespace = AppList.XmlNamespace)]
    public abstract class AccessPoint : XmlUnknown, ICloneable<AccessPoint>
    {
        /// <summary>
        /// Retrieves identifiers from a namespace global to all <see cref="AccessPoint"/>s.
        /// Collisions in this namespace indicate that the respective <see cref="AccessPoint"/>s are in conflict cannot be applied on a system at the same time.
        /// </summary>
        /// <param name="appEntry">The application entry containing this access point.</param>
        /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Store.Model.Capabilities.Capability"/> is invalid.</exception>
        /// <remarks>These identifiers are not guaranteed to stay the same between versions. They should not be stored in files but instead always generated on demand.</remarks>
        public abstract IEnumerable<string> GetConflictIDs(AppEntry appEntry);

        /// <summary>
        /// Applies this access point to the current machine.
        /// </summary>
        /// <param name="appEntry">The application being integrated.</param>
        /// <param name="feed">The feed providing additional metadata, icons, etc. for the application.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="machineWide">Apply the configuration machine-wide instead of just for the current user.</param>
        /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Store.Model.Capabilities.Capability"/> is invalid.</exception>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The access point's data or a referenced <see cref="Store.Model.Capabilities.Capability"/>'s data are invalid.</exception>
        public abstract void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide);

        /// <summary>
        /// Unapply this access point on the current machine.
        /// </summary>
        /// <param name="appEntry">The application entry containing this access point.</param>
        /// <param name="machineWide">Apply the configuration machine-wide instead of just for the current user.</param>
        /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Store.Model.Capabilities.Capability"/> is invalid.</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public abstract void Unapply(AppEntry appEntry, bool machineWide);

        /// <summary>
        /// Creates a deep copy of this <see cref="AccessPoint"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="AccessPoint"/>.</returns>
        public abstract AccessPoint Clone();
    }
}
