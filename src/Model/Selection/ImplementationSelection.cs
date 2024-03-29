// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Selection;

/// <summary>
/// An executable implementation of a <see cref="Feed"/> as a part of a <see cref="Selections"/>.
/// </summary>
/// <remarks>This class does not contain information on how to download the implementation in case it is not in cache. That must be obtained from a <see cref="Implementation"/> instance.</remarks>
/// <seealso cref="Selections.Implementations"/>
[XmlType("selection", Namespace = Feed.XmlNamespace)]
[SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Justification = "IComparable is only used for deterministic ordering")]
[Equatable]
public sealed partial class ImplementationSelection : ImplementationBase, IInterfaceUriBindingContainer, ICloneable<ImplementationSelection>, IComparable<ImplementationSelection>
{
    /// <summary>
    /// The URI or local path of the interface this implementation is for.
    /// </summary>
    [Description("The URI or local path of the interface this implementation is for.")]
    [XmlIgnore]
    public required FeedUri InterfaceUri { get; set; }

    /// <summary>
    /// The URL or local path of the feed that contains this implementation.
    /// <see cref="FeedUri.FromDistributionPrefix"/> is prepended if data is pulled from a native package manager.
    /// If <c>null</c> or <see cref="string.Empty"/> use <see cref="InterfaceUri"/> instead.
    /// </summary>
    [Description("""The URL or local path of the feed that contains this implementation. "distribution:" is prepended if data is pulled from a native package manager. If null or empty use InterfaceUri instead.""")]
    [XmlIgnore]
    public FeedUri? FromFeed { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="InterfaceUri"/>
    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
    [XmlAttribute("interface"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public string InterfaceUriString { get => InterfaceUri?.ToStringRfc()!; set => InterfaceUri = new(value); }

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="FromFeed"/>
    [XmlAttribute("from-feed"), DefaultValue(""), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? FromFeedString { get => FromFeed?.ToStringRfc(); set => FromFeed = value?.To(x => new FeedUri(x)); }

    /// <summary>
    /// Used for XML serialization.
    /// </summary>
    public ImplementationSelection() {}
    #endregion

    /// <summary>
    /// A file which, if present, indicates that the selection is still valid. This is sometimes used with distribution-provided selections. If not present and the ID starts with "package:", you'll need to query the distribution's package manager to check that this version is still installed.
    /// </summary>
    [Description("""A file which, if present, indicates that the selection is still valid. This is sometimes used with distribution-provided selections. If not present and the ID starts with "package:", you'll need to query the distribution's package manager to check that this version is still installed.""")]
    [XmlAttribute("quick-test-file")]
    public string? QuickTestFile { get; set; }

    /// <summary>
    /// All <see cref="Implementation"/>s that were considered by the solver when this one was chosen.
    /// <c>null</c> when selections are loaded from a file.
    /// </summary>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public IReadOnlyList<SelectionCandidate>? Candidates { get; }

    /// <summary>
    /// The name of the distribution (e.g. Debian, RPM) where this implementation comes from, if any.
    /// </summary>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public string? Distribution
        => FromFeed is {IsFromDistribution: true} ? FromFeed.LocalPath : null;

    /// <summary>
    /// Creates a new implementation selection.
    /// </summary>
    /// <param name="candidates">All candidates that were considered for selection (including the selected one). These are used to present the user with possible alternatives.</param>
    public ImplementationSelection(IReadOnlyList<SelectionCandidate> candidates)
    {
        Candidates = candidates;
    }

    /// <inheritdoc/>
    public override void Normalize(FeedUri? feedUri = null)
    {
        base.Normalize(feedUri);

        EnsureAttribute(InterfaceUri, "interface");
    }

    #region Conversion
    /// <inheritdoc/>
    public override string ToString() => $"{base.ToString()} ({InterfaceUri})";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="ImplementationSelection"/>
    /// </summary>
    /// <returns>The cloned <see cref="ImplementationSelection"/>.</returns>
    ImplementationSelection ICloneable<ImplementationSelection>.Clone()
    {
        var implementation = new ImplementationSelection
        {
            InterfaceUri = InterfaceUri,
            FromFeed = FromFeed,
            ID = ID,
            Version = Version,
            QuickTestFile = QuickTestFile
        };
        CloneFromTo(this, implementation);
        return implementation;
    }

    /// <summary>
    /// Creates a deep copy of this <see cref="ImplementationSelection"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ImplementationSelection"/>.</returns>
    public override Element Clone() => ((ICloneable<ImplementationSelection>)this).Clone();
    #endregion

    #region Comparison
    /// <inheritdoc/>
    public int CompareTo(ImplementationSelection? other)
        => StringComparer.Ordinal.Compare(InterfaceUri.ToStringRfc(), other?.InterfaceUri.ToStringRfc());
    #endregion
}
