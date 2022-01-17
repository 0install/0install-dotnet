// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// An exclusion like <c>!2.0</c> as a part of a <see cref="VersionRange"/>.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[Serializable]
[PrimaryConstructor]
public sealed partial class VersionRangePartExclude : VersionRangePart
{
    /// <summary>
    /// The version to be excluded.
    /// </summary>
    public ImplementationVersion Version { get; }

    /// <inheritdoc/>
    public override IEnumerable<VersionRangePart> Intersect(VersionRange versions)
    {
        #region Sanity checks
        if (versions == null) throw new ArgumentNullException(nameof(versions));
        #endregion

        if (versions.Parts.Count == 0)
            yield return this;

        foreach (var part in versions.Parts)
        {
            switch (part)
            {
                case VersionRangePartRange range:
                    if (!range.Match(Version)) yield return range;
                    else throw new NotSupportedException($"Unable to intersect {this} with {range}.");
                    break;

                case VersionRangePartExact exact:
                    if (!exact.Match(Version)) yield return exact;
                    break;

                case VersionRangePartExclude exclude:
                    yield return this;
                    if (!Equals(exclude)) yield return exclude;
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public override bool Match(ImplementationVersion version) => !Version.Equals(version ?? throw new ArgumentNullException(nameof(version)));

    /// <inheritdoc/>
    public override string ToString() => "!" + Version;

    #region Equatable
    public override bool Equals(object? obj)
        => obj is VersionRangePartExclude exclude
        && Version.Equals(exclude.Version);

    public override int GetHashCode() => Version.GetHashCode();
    #endregion
}
