// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// A version range like <c>1.0..!2.0</c> as a part of a <see cref="VersionRange"/>.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[Serializable]
public sealed class VersionRangePartRange : VersionRangePart
{
    /// <summary>
    /// The lower inclusive bound. May be <c>null</c>.
    /// </summary>
    public ImplementationVersion? LowerInclusive { get; }

    /// <summary>
    /// The upper exclusive bound. May be <c>null</c>.
    /// </summary>
    public ImplementationVersion? UpperExclusive { get; }

    /// <summary>
    /// Creates a new version range.
    /// </summary>
    /// <param name="lowerInclusive">The lower inclusive bound. May be <c>null</c>.</param>
    /// <param name="upperExclusive">The upper exclusive bound. May be <c>null</c>.</param>
    public VersionRangePartRange(ImplementationVersion? lowerInclusive, ImplementationVersion? upperExclusive)
    {
        LowerInclusive = lowerInclusive;
        UpperExclusive = upperExclusive;
    }

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
                    var lowerInclusive = (LowerInclusive == null) || (range.LowerInclusive != null && range.LowerInclusive > LowerInclusive) ? range.LowerInclusive : LowerInclusive;
                    var upperExclusive = (UpperExclusive == null) || (range.UpperExclusive != null && range.UpperExclusive < UpperExclusive) ? range.UpperExclusive : UpperExclusive;
                    if (lowerInclusive == null || upperExclusive == null || lowerInclusive < upperExclusive)
                        yield return new VersionRangePartRange(lowerInclusive, upperExclusive);
                    break;

                case VersionRangePartExact exact:
                    if (Match(exact.Version)) yield return exact;
                    break;

                case VersionRangePartExclude exclude:
                    if (!Match(exclude.Version)) yield return this;
                    else throw new NotSupportedException($"Unable to intersect {this} with {exclude}.");
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public override bool Match(ImplementationVersion version)
    {
        if (version == null) throw new ArgumentNullException(nameof(version));
        return (LowerInclusive == null || version >= LowerInclusive)
            && (UpperExclusive == null || version < UpperExclusive);
    }

    /// <inheritdoc/>
    public override string ToString()
        => LowerInclusive + ".." + (UpperExclusive == null ? "" : "!" + UpperExclusive);

    #region Equatable
    public override bool Equals(object? obj)
        => obj is VersionRangePartRange other
        && Equals(LowerInclusive, other.LowerInclusive)
        && Equals(UpperExclusive, other.UpperExclusive);

    public override int GetHashCode()
        => HashCode.Combine(LowerInclusive, UpperExclusive);
    #endregion
}
