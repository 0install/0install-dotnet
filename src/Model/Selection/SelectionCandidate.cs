// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Properties;

namespace ZeroInstall.Model.Selection
{
    /// <summary>
    /// Represents an <see cref="Implementation"/> that is available to a solver for selection.
    /// </summary>
    public sealed class SelectionCandidate : IEquatable<SelectionCandidate>
    {
        /// <summary>
        /// The implementation this selection candidate references.
        /// </summary>
        [Browsable(false)]
        public Implementation Implementation { get; }

        /// <summary>
        /// The file name or URL of the feed listing the implementation.
        /// </summary>
        public FeedUri FeedUri { get; }

        /// <summary>
        /// The <see cref="FeedPreferences"/> for <see cref="FeedUri"/>.
        /// </summary>
        [Browsable(false)]
        public FeedPreferences FeedPreferences { get; }

        /// <summary>
        /// The version number of the implementation.
        /// </summary>
        [Description("The version number of the implementation.")]
        public ImplementationVersion Version => Implementation.Version;

        /// <summary>
        /// The date this implementation was made available. For development versions checked out from version control this attribute should not be present.
        /// </summary>
        [Description("The date this implementation was made available. For development versions checked out from version control this attribute should not be present.")]
        public DateTime Released => Implementation.Released;

        /// <summary>
        /// The default stability rating for this implementation.
        /// </summary>
        [Description("The default stability rating for this implementation.")]
        public Stability Stability => Implementation.Stability;

        /// <summary>The preferences controlling how the solver evaluates this candidate.</summary>
        private readonly ImplementationPreferences _implementationPreferences;

        /// <summary>
        /// A user-specified override for the <see cref="Stability"/> specified in the feed.
        /// </summary>
        [Description("A user-specified override for the stability specified in the feed.")]
        public Stability UserStability { get => _implementationPreferences.UserStability; set => _implementationPreferences.UserStability = value; }

        /// <summary>
        /// The <see cref="UserStability"/> if it is set, otherwise <see cref="Stability"/>.
        /// </summary>
        [Browsable(false)]
        public Stability EffectiveStability => (UserStability == Stability.Unset) ? Stability : UserStability;

        /// <summary>
        /// For platform-specific binaries, the platform for which an <see cref="Implementation"/> was compiled, in the form os-cpu. Either the os or cpu part may be *, which will make it available on any OS or CPU.
        /// </summary>
        [Description("For platform-specific binaries, the platform for which an implementation was compiled, in the form os-cpu. Either the os or cpu part may be *, which will make it available on any OS or CPU.")]
        public string Architecture => Implementation.Architecture.ToString();

        /// <summary>
        /// Human-readable notes about the implementation, e.g. "not suitable for this architecture".
        /// </summary>
        [Description("Human-readable notes about the implementation, e.g. \"not suitable for this architecture\".")]
        public string? Notes { get; }

        /// <summary>
        /// Indicates whether this implementation fulfills all specified <see cref="Requirements"/>.
        /// </summary>
        [Browsable(false)]
        public bool IsSuitable { get; }

        /// <summary>
        /// Creates a new selection candidate.
        /// </summary>
        /// <param name="feedUri">The file name or URL of the feed listing the implementation.</param>
        /// <param name="feedPreferences">The <see cref="FeedPreferences"/> for <see cref="FeedUri"/>.</param>
        /// <param name="implementation">The implementation this selection candidate references.</param>
        /// <param name="requirements">A set of requirements/restrictions the <paramref name="implementation"/> needs to fulfill for <see cref="IsSuitable"/> to be <c>true</c>.</param>
        /// <param name="offlineUncached">Mark this candidate as unsuitable because it is uncached and the network mode is set to offline.</param>
        public SelectionCandidate(FeedUri feedUri, FeedPreferences feedPreferences, Implementation implementation, Requirements requirements, bool offlineUncached = false)
        {
            FeedUri = feedUri ?? throw new ArgumentNullException(nameof(feedUri));
            FeedPreferences = feedPreferences ?? throw new ArgumentNullException(nameof(feedPreferences));
            Implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));

            _implementationPreferences = feedPreferences[implementation.ID];

            if (Implementation.Architecture.Cpu == Cpu.Source && requirements.Architecture.Cpu != Cpu.Source)
                Notes = Resources.SelectionCandidateNoteSource;
            else if (!Implementation.Architecture.RunsOn(requirements.Architecture))
                Notes = Resources.SelectionCandidateNoteIncompatibleArchitecture;
            else if (!Match(requirements, Version))
                Notes = Resources.SelectionCandidateNoteVersionMismatch;
            else if (EffectiveStability == Stability.Buggy)
                Notes = Resources.SelectionCandidateNoteBuggy;
            else if (EffectiveStability == Stability.Insecure)
                Notes = Resources.SelectionCandidateNoteInsecure;
            else if (!Implementation.ContainsCommand(requirements.Command ?? Command.NameRun))
                Notes = string.Format(Resources.SelectionCandidateNoteCommand, requirements.Command);
            else if (offlineUncached)
                Notes = Resources.SelectionCandidateNoteNotCached;
            else IsSuitable = true;
        }

        private static bool Match(Requirements requirements, ImplementationVersion version)
            => !requirements.ExtraRestrictions.TryGetValue(requirements.InterfaceUri, out var range) || range.Match(version);

        #region Conversion
        /// <summary>
        /// Returns the selection candidate in the form "SelectionCandidate: Implementation". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"SelectionCandidate: {Implementation} from {FeedUri}";
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(SelectionCandidate? other)
            => other != null
            && FeedUri.Equals(other.FeedUri)
            && Implementation.ID == other.Implementation.ID;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is SelectionCandidate candidate && Equals(candidate);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(FeedUri, Implementation.ID);
        #endregion
    }
}
