// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Values.Design;

namespace ZeroInstall.Model;

/// <summary>
/// Represents a (possibly disjoint) set of ranges of <see cref="ImplementationVersion"/>s.
/// </summary>
/// <remarks>
/// <para>This class is immutable.</para>
/// <para>
///   Ranges are separated by pipes (|).
///   Each range is in the form "START..!END". The range matches versions where START &lt;= VERSION &lt; END.
///   The start or end may be omitted. A single version number may be used instead of a range to match only that version,
///   or !VERSION to match everything except that version.
/// </para>
/// </remarks>
[TypeConverter(typeof(StringConstructorConverter<VersionRange>))]
[Serializable]
[Equatable]
public sealed partial class VersionRange
{
    /// <summary>
    /// An "impossible" range matching no versions.
    /// </summary>
    public static readonly VersionRange None = new(new VersionRangePartRange(new("0"), new("0")));

    /// <summary>
    /// The individual ranges.
    /// </summary>
    [OrderedEquality]
    public IReadOnlyList<VersionRangePart> Parts { get; }

    /// <summary>
    /// Creates an empty version range (matches everything).
    /// </summary>
    public VersionRange()
    {
        Parts = [];
    }

    /// <summary>
    /// Creates a new version range set.
    /// </summary>
    /// <param name="parts">The individual ranges.</param>
    public VersionRange(params IEnumerable<VersionRangePart> parts)
    {
        Parts = parts.ToArray();
    }

    /// <summary>
    /// Creates a new version range set from a a string.
    /// </summary>
    /// <param name="value">The string containing the version ranges.</param>
    /// <exception cref="FormatException"><paramref name="value"/> is not a valid version range string.</exception>
    public VersionRange(string value)
        : this(value.Split('|').Select(part => VersionRangePart.Parse(part.Trim())))
    {}

    /// <summary>
    /// Convenience cast for <see cref="ImplementationVersion"/>s into <see cref="VersionRange"/>s that match that exact version.
    /// </summary>
    [return: NotNullIfNotNull("version")]
    public static implicit operator VersionRange?(ImplementationVersion? version)
        => version?.To(x => new VersionRange(new VersionRangePartExact(x)));

    /// <summary>
    /// Convenience cast for <see cref="Constraint"/>s into <see cref="VersionRange"/>s.
    /// </summary>
    [return: NotNullIfNotNull("constraint")]
    public static implicit operator VersionRange?(Constraint? constraint)
        => constraint?.To(x => new VersionRange(new VersionRangePartRange(x.NotBefore, x.Before)));

    /// <summary>
    /// Creates a new <see cref="VersionRange"/> using the specified string representation.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">Returns the created <see cref="VersionRange"/> if successfully; <c>null</c> otherwise.</param>
    /// <returns><c>true</c> if the <see cref="VersionRange"/> was successfully created; <c>false</c> otherwise.</returns>
    public static bool TryCreate(
        string value,
        [NotNullWhen(true)] out VersionRange? result)
    {
        try
        {
            result = new VersionRange(value);
            return true;
        }
        catch (ArgumentException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Intersects another version range set with this one and returns a new set as the result.
    /// </summary>
    public VersionRange Intersect(VersionRange other)
    {
        if (Parts is []) return other;

        var parts = Parts.SelectMany(x => x.Intersect(other)).Distinct().ToList();
        return parts.Count == 0 ? None : new(parts);
    }

    /// <summary>
    /// Determines whether a specific version lies within this range set.
    /// </summary>
    public bool Match(ImplementationVersion version)
        => Parts is []
        || Parts.Any(part => part.Match(version));

    /// <summary>
    /// Returns a string representation of the version range set. Safe for parsing!
    /// </summary>
    public override string ToString()
        => string.Join("|", Parts.Select(part => part.ToString()).WhereNotNull());
}
