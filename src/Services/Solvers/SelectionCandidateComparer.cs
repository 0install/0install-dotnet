// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Ranks <see cref="SelectionCandidate"/>s.
/// </summary>
/// <param name="stabilityPolicy">Implementations at this stability level or higher are preferred. Lower levels are used only if there is no other choice.</param>
/// <param name="networkUse">Controls how liberally network access is attempted.</param>
/// <param name="languages">The preferred languages for the implementation.</param>
/// <param name="isCached">sed to determine which implementations are already cached in the <see cref="IImplementationStore"/>.</param>
public sealed class SelectionCandidateComparer(
    Stability stabilityPolicy,
    NetworkLevel networkUse,
    LanguageSet languages,
    Predicate<Implementation> isCached) : IComparer<SelectionCandidate>
{
    /// <inheritdoc/>
    public int Compare(SelectionCandidate? x, SelectionCandidate? y)
    {
        if (ReferenceEquals(
                x ?? throw new ArgumentNullException(nameof(x)),
                y ?? throw new ArgumentNullException(nameof(y)))) return 0;

        // Preferred implementations come first
        if (x.EffectiveStability == Stability.Preferred && y.EffectiveStability != Stability.Preferred) return -1;
        if (x.EffectiveStability != Stability.Preferred && y.EffectiveStability == Stability.Preferred) return 1;

        // Strongly prefer languages we understand
        int xLanguageRank = GetLanguageRank(x.Implementation.Languages);
        int yLanguageRank = GetLanguageRank(y.Implementation.Languages);
        if (xLanguageRank > yLanguageRank) return -1;
        if (xLanguageRank < yLanguageRank) return 1;

        // Cached implementations come next if we have limited network access
        if (networkUse < NetworkLevel.Full)
        {
            bool xCached = isCached(x.Implementation);
            bool yCached = isCached(y.Implementation);
            if (xCached && !yCached) return -1;
            if (!xCached && yCached) return 1;
        }

        // TODO: Packages that require admin access to install come last

        // Prefer more stable versions, but treat everything over the stability policy the same
        // (so we prefer stable over testing if the policy is to prefer "stable", otherwise we don't care)
        var xStability = CapToPolicy(x.EffectiveStability);
        var yStability = CapToPolicy(y.EffectiveStability);
        if (xStability < yStability) return -1;
        if (xStability > yStability) return 1;

        // Newer versions come before older ones (ignoring modifiers)
        if (x.Version.FirstPart > y.Version.FirstPart) return -1;
        if (x.Version.FirstPart < y.Version.FirstPart) return 1;

        // Prefer native packages if the main part of the versions are the same
        if (x.Stability == Stability.Packaged && y.Stability != Stability.Packaged) return -1;
        if (y.Stability == Stability.Packaged && x.Stability != Stability.Packaged) return 1;

        // Full version compare (after package check, since comparing modifiers between native and non-native packages doesn't make sense)
        if (x.Version > y.Version) return -1;
        if (x.Version < y.Version) return 1;

        // More specific OS types come first (checking whether the OS type is compatible at all is done elsewhere)
        if (x.Implementation.Architecture.OS > y.Implementation.Architecture.OS) return -1;
        if (x.Implementation.Architecture.OS < y.Implementation.Architecture.OS) return 1;

        // More specific CPU types come first (checking whether the CPU type is compatible at all is done elsewhere)
        if (x.Implementation.Architecture.Cpu > y.Implementation.Architecture.Cpu) return -1;
        if (x.Implementation.Architecture.Cpu < y.Implementation.Architecture.Cpu) return 1;

        // Slightly prefer languages specialized to our country
        int xCountryRank = GetCountryRank(x.Implementation.Languages);
        int yCountryRank = GetCountryRank(y.Implementation.Languages);
        if (xCountryRank > yCountryRank) return -1;
        if (xCountryRank < yCountryRank) return 1;

        // Slightly prefer cached versions
        if (networkUse == NetworkLevel.Full)
        {
            bool xCached = isCached(x.Implementation);
            bool yCached = isCached(y.Implementation);
            if (xCached && !yCached) return -1;
            if (!xCached && yCached) return 1;
        }

        // Order by ID so the order is not random
        return string.CompareOrdinal(x.Implementation.ID, y.Implementation.ID);
    }

    private Stability CapToPolicy(Stability stability)
        => (stability <= stabilityPolicy) ? Stability.Preferred : stability;

    private static readonly LanguageSet _englishSet = new() {"en", "en_US"};

    private int GetLanguageRank(LanguageSet other)
    {
        if (other.Count == 0) return 1;
        else if (other.ContainsAny(languages, ignoreCountry: true)) return 2;
        else if (other.ContainsAny(_englishSet)) return 0; // Prefer English over other languages we do not understand
        else return -1;
    }

    private int GetCountryRank(LanguageSet other)
    {
        if (other.Count == 0) return 0;
        else if (other.ContainsAny(languages)) return 1;
        else return -1;
    }
}
