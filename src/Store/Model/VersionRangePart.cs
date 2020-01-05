// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using NanoByte.Common;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Represents an individual non-disjoint part of a <see cref="VersionRange"/>.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    public abstract class VersionRangePart
    {
        /// <summary>
        /// Parses a string into a <see cref="VersionRange"/> part.
        /// </summary>
        /// <exception cref="FormatException"><paramref name="value"/> is not a valid version range string.</exception>
        public static VersionRangePart FromString(string value)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            #endregion

            if (value.Contains(".."))
            {
                string start = value.GetLeftPartAtFirstOccurrence("..");
                var startVersion = string.IsNullOrEmpty(start) ? null : new ImplementationVersion(start);

                ImplementationVersion? endVersion;
                string end = value.GetRightPartAtFirstOccurrence("..");
                if (string.IsNullOrEmpty(end)) endVersion = null;
                else
                {
                    if (!end.StartsWith("!")) throw new FormatException(string.Format(Resources.VersionRangeEndNotExclusive, end));
                    endVersion = new ImplementationVersion(end.Substring(1));
                }

                return new VersionRangePartRange(startVersion, endVersion);
            }
            else if (value.StartsWith("!"))
            {
                return new VersionRangePartExclude(
                    new ImplementationVersion(value.Substring(1)));
            }
            else
            {
                return new VersionRangePartExact(
                    new ImplementationVersion(value));
            }
        }

        /// <summary>
        /// Intersects a set of version ranges with this individual range and returns the surviving parts.
        /// </summary>
        public abstract IEnumerable<VersionRangePart> Intersect(VersionRange versions);

        /// <summary>
        /// Determines whether a specific version lies within this range.
        /// </summary>
        public abstract bool Match(ImplementationVersion version);
    }
}
