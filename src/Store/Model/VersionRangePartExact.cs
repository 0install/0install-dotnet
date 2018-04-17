// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// An exact version number like <c>2.0</c> as a part of a <see cref="VersionRange"/>.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    public sealed class VersionRangePartExact : VersionRangePart
    {
        public ImplementationVersion Version { get; }

        /// <summary>
        /// Creates a new exact version.
        /// </summary>
        /// <param name="version">The exact version to match.</param>
        public VersionRangePartExact(ImplementationVersion version)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        /// <inheritdoc/>
        public override IEnumerable<VersionRangePart> Intersect(VersionRange versions)
        {
            #region Sanity checks
            if (versions == null) throw new ArgumentNullException(nameof(versions));
            #endregion

            if (versions.Match(Version)) yield return this;
        }

        /// <inheritdoc/>
        public override bool Match(ImplementationVersion version) => Version.Equals(version ?? throw new ArgumentNullException(nameof(version)));

        /// <inheritdoc/>
        public override string ToString() => Version.ToString();

        #region Equality
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is VersionRangePartExact exact && Version.Equals(exact.Version);
        }

        public override int GetHashCode() => Version.GetHashCode();
        #endregion
    }
}
