// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NanoByte.Common.Collections;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Ranks <see cref="SelectionCandidate"/>s.
    /// </summary>
    public class SelectionCandidateComparer : IComparer<SelectionCandidate>
    {
        private readonly Stability _stabilityPolicy;
        private readonly NetworkLevel _networkUse;
        private readonly LanguageSet _languages;
        private readonly Predicate<Implementation> _isCached;

        /// <summary>
        /// Creates a new <see cref="SelectionCandidate"/> ranker.
        /// </summary>
        /// <param name="stabilityPolicy">Implementations at this stability level or higher are preferred. Lower levels are used only if there is no other choice.</param>
        /// <param name="networkUse">Controls how liberally network access is attempted.</param>
        /// <param name="languages">The preferred languages for the implementation.</param>
        /// <param name="isCached">Used to determine which implementations are already cached in the <see cref="IStore"/>.</param>
        public SelectionCandidateComparer(Stability stabilityPolicy, NetworkLevel networkUse, [NotNull] LanguageSet languages, [NotNull] Predicate<Implementation> isCached)
        {
            _stabilityPolicy = stabilityPolicy;
            _networkUse = networkUse;
            _languages = languages ?? throw new ArgumentNullException(nameof(languages));
            _isCached = isCached ?? throw new ArgumentNullException(nameof(isCached));
        }

        /// <inheritdoc/>
        public int Compare(SelectionCandidate x, SelectionCandidate y)
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
            if (_networkUse != NetworkLevel.Full)
            {
                bool xCached = _isCached(x.Implementation);
                bool yCached = _isCached(y.Implementation);
                if (xCached && !yCached) return -1;
                if (!xCached && yCached) return 1;
            }

            // TODO: Packages that require admin access to install come last

            // Prefer more stable versions, but treat everything over the stability policy the same
            // (so we prefer stable over testing if the policy is to prefer "stable", otherwise we don't care)
            var xStability = ApplyPolicy(x.EffectiveStability);
            var yStability = ApplyPolicy(y.EffectiveStability);
            if (xStability < yStability) return -1;
            if (xStability > yStability) return 1;

            // Newer versions come before older ones (ignoring modifiers)
            if (x.Version.FirstPart > y.Version.FirstPart) return -1;
            if (x.Version.FirstPart < y.Version.FirstPart) return 1;

            // Prefer native packages if the main part of the versions are the same
            if (x.EffectiveStability == Stability.Packaged && y.EffectiveStability != Stability.Packaged) return -1;
            if (y.EffectiveStability == Stability.Packaged && x.EffectiveStability != Stability.Packaged) return 1;

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
            if (_networkUse == NetworkLevel.Full)
            {
                bool xCached = _isCached(x.Implementation);
                bool yCached = _isCached(y.Implementation);
                if (xCached && !yCached) return -1;
                if (!xCached && yCached) return 1;
            }

            // Order by ID so the order is not random
            return string.CompareOrdinal(x.Implementation.ID, y.Implementation.ID);
        }

        private Stability ApplyPolicy(Stability stability)
            => (stability <= _stabilityPolicy) ? Stability.Preferred : stability;

        private static readonly LanguageSet _englishSet = new LanguageSet {"en", "en_US"};

        private int GetLanguageRank(LanguageSet languages)
        {
            if (languages.Count == 0) return 1;
            else if (languages.ContainsAny(_languages, ignoreCountry: true)) return 2;
            else if (languages.ContainsAny(_englishSet)) return 0; // Prefer English over other languages we do not understand
            else return -1;
        }

        private int GetCountryRank(LanguageSet languages)
        {
            if (languages.Count == 0) return 0;
            else if (languages.ContainsAny(_languages)) return 1;
            else return -1;
        }
    }
}
