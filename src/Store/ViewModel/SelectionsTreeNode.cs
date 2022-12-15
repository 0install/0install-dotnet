// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models an <see cref="ImplementationSelection"/> element of a <see cref="Selections"/> document for display in a tree-like UI.
/// </summary>
/// <param name="Uri">The feed URI of the selected implementation.</param>
/// <param name="Version">The version of the selected implementation. <c>null</c> for a missing selection.</param>
/// <param name="Path">The local path of the cached implementation. <c>null</c> for an uncached implementation.</param>
/// <param name="Parent">The parent node containing this one. <c>null</c> for root node.</param>
public sealed record SelectionsTreeNode(
        [property: DisplayName("URI")] FeedUri Uri,
        ImplementationVersion? Version,
        string? Path,
        [property: Browsable(false)] SelectionsTreeNode? Parent)
    : INamed
{
    /// <summary>
    /// The full name of the node used for tree hierarchies.
    /// </summary>
    [Browsable(false)]
    public string Name
    {
        get => Parent == null ? ToString() : Parent.Name + Named.TreeSeparator + ToString();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
    {
        string result = $"{Uri}: {Version?.ToString() ?? Resources.NoSelectedVersion}";
        if (Path != null) result += $" ({Path})";
        return result;
    }
}
