// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using Generator.Equals;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models an implementation change between two <see cref="Selections"/> documents for display in a UI.
/// </summary>
[Equatable]
public sealed partial class SelectionsDiffNode
{
    /// <summary>
    /// The interface URI of the changed implementation.
    /// </summary>
    [DisplayName("URI")]
    public FeedUri Uri { get; }

    /// <summary>
    /// The old version of the implementation; <c>null</c> if the implementation did not exist in the old selection.
    /// </summary>
    [DisplayName("Old version")]
    public ImplementationVersion? OldVersion { get; }

    /// <summary>
    /// The new version of the implementation; <c>null</c> if the implementation does not exist in the new selection.
    /// </summary>
    [DisplayName("New version")]
    public ImplementationVersion? NewVersion { get; }

    /// <summary>
    /// Creates a new selection diff.
    /// </summary>
    /// <param name="uri">The interface URI of the changed implementation.</param>
    /// <param name="oldVersion">The old version of the implementation; <c>null</c> if the implementation did not exist in the old selection.</param>
    /// <param name="newVersion">The new version of the implementation; <c>null</c> if the implementation does not exist in the new selection.</param>
    public SelectionsDiffNode(FeedUri uri, ImplementationVersion? oldVersion = null, ImplementationVersion? newVersion = null)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        OldVersion = oldVersion;
        NewVersion = newVersion;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (OldVersion == null) return $"{Uri}: new -> {NewVersion}";
        else if (NewVersion == null) return $"{Uri}: removed";
        else return $"{Uri}: {OldVersion} -> {NewVersion}";
    }
}
