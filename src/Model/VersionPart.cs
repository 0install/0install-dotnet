// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Represents a part of a <see cref="ImplementationVersion"/> containing nothing, a <see cref="VersionModifier"/>, a <see cref="DottedList"/> or both.
/// </summary>
[Serializable]
public readonly struct VersionPart : IEquatable<VersionPart>, IComparable<VersionPart>
{
    /// <summary>
    /// The optional modifier prefix.
    /// </summary>
    public VersionModifier Modifier { get; }

    /// <summary>
    /// The dotted list part.
    /// </summary>
    public VersionDottedList DottedList { get; }

    /// <summary>
    /// Creates a new version part.
    /// </summary>
    /// <param name="modifier">The optional modifier prefix.</param>
    /// <param name="dottedList">The dotted list part.</param>
    public VersionPart(VersionModifier modifier = VersionModifier.None, VersionDottedList dottedList = default)
    {
        Modifier = modifier;
        DottedList = dottedList;
    }

    /// <summary>
    /// A version part with a value lower than "0".
    /// </summary>
    internal static readonly VersionPart Unset = new(dottedList: new(-1));

    /// <summary>
    /// Parses a string into a version part.
    /// </summary>
    /// <exception cref="FormatException"><paramref name="value"/> is not a valid version part.</exception>
    public static VersionPart Parse(string value)
    {
        #region Sanity checks
        if (value == null) throw new ArgumentNullException(nameof(value));
        #endregion

        VersionModifier modifier;
        if (value.StartsWith("pre", out string? trimmed))
            modifier = VersionModifier.Pre;
        else if (value.StartsWith("rc", out trimmed))
            modifier = VersionModifier.RC;
        else if (value.StartsWith("post", out trimmed))
            modifier = VersionModifier.Post;
        else
        {
            trimmed = value;
            modifier = VersionModifier.None;
        }

        return new(modifier, string.IsNullOrEmpty(trimmed) ? default : VersionDottedList.Parse(trimmed));
    }

    #region Conversion
    /// <inheritdoc/>
    public override string ToString()
        => Modifier switch
        {
            VersionModifier.None => "",
            VersionModifier.Pre => "pre",
            VersionModifier.RC => "rc",
            VersionModifier.Post => "post",
            _ => throw new InvalidOperationException(Resources.UnknownModifier)
        } + DottedList;
    #endregion

    #region Equatable
    /// <inheritdoc/>
    public bool Equals(VersionPart other)
        => Modifier == other.Modifier
        && DottedList == other.DottedList;

    public static bool operator ==(VersionPart left, VersionPart right) => left.Equals(right);
    public static bool operator !=(VersionPart left, VersionPart right) => !left.Equals(right);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is VersionPart part && Equals(part);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Modifier, DottedList);
    #endregion

    #region Comparison
    public int CompareTo(VersionPart other)
    {
        int modifierComparison = ((int)Modifier).CompareTo((int)other.Modifier);
        if (modifierComparison != 0) return modifierComparison;

        return DottedList.CompareTo(other.DottedList);
    }

    public static bool operator <(VersionPart left, VersionPart right) => left.CompareTo(right) < 0;
    public static bool operator >(VersionPart left, VersionPart right) => left.CompareTo(right) > 0;
    public static bool operator <=(VersionPart left, VersionPart right) => left.CompareTo(right) <= 0;
    public static bool operator >=(VersionPart left, VersionPart right) => left.CompareTo(right) >= 0;
    #endregion
}
