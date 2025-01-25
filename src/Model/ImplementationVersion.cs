// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Values.Design;

namespace ZeroInstall.Model;

/// <summary>
/// Represents a version number consisting of dot-separated decimals and optional modifier strings.
/// </summary>
/// <remarks>
/// <para>This class is immutable and thread-safe.</para>
/// <para>
///   This is the syntax for valid version strings:
///   <code>
///   Version := DottedList ("-" Modifier? DottedList?)*
///   DottedList := (Integer ("." Integer)*)
///   Modifier := "pre" | "rc" | "post"
///   </code>
///   If the string <see cref="ModelUtils.ContainsTemplateVariables"/> the entire string is stored verbatim and not parsed.
/// </para>
/// </remarks>
[TypeConverter(typeof(StringConstructorConverter<ImplementationVersion>))]
[Serializable]
[Equatable]
public sealed partial class ImplementationVersion : IComparable<ImplementationVersion>
{
    /// <summary>
    /// The first part of the version number.
    /// </summary>
    public VersionDottedList FirstPart { get; }

    /// <summary>
    /// All additional parts of the version number.
    /// </summary>
    [OrderedEquality]
    public IReadOnlyList<VersionPart> AdditionalParts { get; } = [];

    /// <summary>Used to store the unparsed input string (instead of <see cref="FirstPart"/> and <see cref="AdditionalParts"/>) if it <see cref="ModelUtils.ContainsTemplateVariables"/>.</summary>
    private readonly string? _verbatimString;

    /// <summary>
    /// Indicates whether this version number contains a template variable (a substring enclosed in curly brackets, e.g {var}) .
    /// </summary>
    /// <remarks>This must be <c>false</c> in regular feeds; <c>true</c> is only valid for templates.</remarks>
    [Browsable(false), IgnoreEquality]
    public bool ContainsTemplateVariables => _verbatimString != null;

    /// <summary>
    /// Creates a new implementation version.
    /// </summary>
    /// <param name="firstPart">The first part of the version number.</param>
    /// <param name="additionalParts">All additional parts of the version number.</param>
    public ImplementationVersion(VersionDottedList firstPart, params VersionPart[] additionalParts)
    {
        FirstPart = firstPart;
        AdditionalParts = additionalParts ?? throw new ArgumentNullException(nameof(additionalParts));
    }

    /// <summary>
    /// Creates a new implementation version from a a string.
    /// </summary>
    /// <param name="value">The string containing the version information.</param>
    /// <exception cref="FormatException"><paramref name="value"/> is not a valid version string.</exception>
    public ImplementationVersion(string value)
    {
        if (string.IsNullOrEmpty(value)) throw new FormatException(Resources.MustStartWithDottedList);

        if (ModelUtils.ContainsTemplateVariables(value))
        {
            _verbatimString = value;
            return;
        }

        var parts = value.Split('-');

        // Ensure the first part is a dotted list
        if (!VersionDottedList.IsValid(parts[0])) throw new FormatException(Resources.MustStartWithDottedList);
        FirstPart = VersionDottedList.Parse(parts[0]);

        // Iterate through all additional parts
        var additionalParts = new VersionPart[parts.Length - 1];
        for (int i = 1; i < parts.Length; i++)
            additionalParts[i - 1] = VersionPart.Parse(parts[i]);
        AdditionalParts = additionalParts;
    }

    /// <summary>
    /// Creates a new implementation version from a .NET <see cref="Version"/>.
    /// </summary>
    /// <param name="version">The .NET <see cref="Version"/> to convert.</param>
    public ImplementationVersion(Version version)
    {
        #region Sanity checks
        if (version == null) throw new ArgumentNullException(nameof(version));
        #endregion

        FirstPart = VersionDottedList.Parse(version.ToString());
    }

    /// <summary>
    /// Creates a new <see cref="ImplementationVersion"/> using the specified string representation.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">Returns the created <see cref="ImplementationVersion"/> if successfully; <c>null</c> otherwise.</param>
    /// <returns><c>true</c> if the <see cref="ImplementationVersion"/> was successfully created; <c>false</c> otherwise.</returns>
    public static bool TryCreate(
        string value,
        [NotNullWhen(true)] out ImplementationVersion? result)
    {
        try
        {
            result = new(value);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    #region Conversion
    /// <summary>
    /// Returns a string representation of the version. Safe for parsing!
    /// </summary>
    public override string ToString()
    {
        if (_verbatimString != null) return _verbatimString;

        var output = new StringBuilder();
        output.Append(FirstPart);

        // Separate additional parts with hyphens
        foreach (var part in AdditionalParts)
        {
            output.Append('-');
            output.Append(part);
        }

        return output.ToString();
    }
    #endregion

    #region Comparison
    /// <inheritdoc/>
    public int CompareTo(ImplementationVersion? other) => Compare(this, other);

    public static bool operator <(ImplementationVersion? left, ImplementationVersion? right) => Compare(left, right) < 0;
    public static bool operator >(ImplementationVersion? left, ImplementationVersion? right) => Compare(left, right) > 0;
    public static bool operator <=(ImplementationVersion? left, ImplementationVersion? right) => Compare(left, right) <= 0;
    public static bool operator >=(ImplementationVersion? left, ImplementationVersion? right) => Compare(left, right) >= 0;

    private static int Compare(ImplementationVersion? left, ImplementationVersion? right)
    {
        if (left == right) return 0;
        if (left == null) return int.MinValue;
        if (right == null) return int.MaxValue;

        int firstPartCompared = left.FirstPart.CompareTo(right.FirstPart);
        if (firstPartCompared != 0) return firstPartCompared;

        int leastNumberOfAdditionalParts = Math.Max(left.AdditionalParts.Count, right.AdditionalParts.Count);
        for (int i = 0; i < leastNumberOfAdditionalParts; ++i)
        {
            VersionPart GetPart(ImplementationVersion version)
                => i >= version.AdditionalParts.Count ? VersionPart.Unset : version.AdditionalParts[i];

            if (GetPart(left).CompareTo(GetPart(right)) is not 0 and var comparisonResult)
                return comparisonResult;
        }
        return 0;
    }
    #endregion
}
