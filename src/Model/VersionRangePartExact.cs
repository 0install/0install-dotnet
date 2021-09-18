// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;

namespace ZeroInstall.Model
{
    /// <summary>
    /// An exact version number like <c>2.0</c> as a part of a <see cref="VersionRange"/>.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    [PrimaryConstructor]
    public sealed partial class VersionRangePartExact : VersionRangePart
    {
        /// <summary>
        /// The exact version to match.
        /// </summary>
        public ImplementationVersion Version { get; }

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

        #region Equatable
        public override bool Equals(object? obj)
            => obj is VersionRangePartExact exact
            && Version.Equals(exact.Version);

        public override int GetHashCode() => Version.GetHashCode();
        #endregion
    }
}
