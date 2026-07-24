// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.Merging;

/// <summary>
/// How well a <see cref="Group"/> matches an <see cref="Implementation"/> that is to be added to it. Higher is better.
/// </summary>
/// <param name="Match"><c>1</c> if the group is compatible at all; <c>0</c> otherwise.</param>
/// <param name="Specificity">The number of requirements and commands the group shares with the implementation.</param>
/// <param name="Attributes">The number of attributes and matching commands the group shares with the implementation.</param>
internal sealed record MatchScore(int Match, int Specificity, int Attributes) : IComparable<MatchScore>
{
    public static readonly MatchScore NoMatch = new(0, 0, 0);

    public int CompareTo(MatchScore? other)
    {
        if (other == null) return 1;
        if (Match != other.Match) return Match.CompareTo(other.Match);
        if (Specificity != other.Specificity) return Specificity.CompareTo(other.Specificity);
        return Attributes.CompareTo(other.Attributes);
    }
}
