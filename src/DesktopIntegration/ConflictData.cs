// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Stores information about an <see cref="AccessPoint"/> conflict.
    /// </summary>
    public readonly struct ConflictData : IEquatable<ConflictData>
    {
        /// <summary>
        /// The <see cref="AccessPoints.AccessPoint"/> causing the conflict.
        /// </summary>
        public readonly AccessPoint AccessPoint;

        /// <summary>
        /// The application containing the <see cref="AccessPoint"/>.
        /// </summary>
        public readonly AppEntry AppEntry;

        /// <summary>
        /// Creates a new conflict data element.
        /// </summary>
        /// <param name="accessPoint">The <see cref="AccessPoints.AccessPoint"/> causing the conflict.</param>
        /// <param name="appEntry">The application containing the <paramref name="accessPoint"/>.</param>
        public ConflictData(AccessPoint accessPoint, AppEntry appEntry)
        {
            AppEntry = appEntry;
            AccessPoint = accessPoint;
        }

        /// <summary>
        /// Returns the entry in the form "AccessPoint in AppEntry". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{AccessPoint} in {AppEntry}";

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ConflictData other)
        {
            if (AppEntry == null || other.AppEntry == null || AccessPoint == null) return false;

            return
                AccessPoint.Equals(other.AccessPoint) &&
                AppEntry.InterfaceUri == other.AppEntry.InterfaceUri;
        }

        public static bool operator ==(ConflictData left, ConflictData right) => left.Equals(right);
        public static bool operator !=(ConflictData left, ConflictData right) => !left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            return obj is ConflictData data && Equals(data);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(AccessPoint, AppEntry?.InterfaceUri);
        #endregion
    }
}
