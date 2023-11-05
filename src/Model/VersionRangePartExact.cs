// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// An exact version number like <c>2.0</c> as a part of a <see cref="VersionRange"/>.
/// </summary>
/// <param name="Version">The exact version to match.</param>
public sealed record VersionRangePartExact(ImplementationVersion Version) : VersionRangePart
{
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
}
