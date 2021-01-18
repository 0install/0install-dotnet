// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using NanoByte.Common;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Models an <see cref="ImplementationSelection"/> element of a <see cref="Selections"/> document for display in a tree-like UI.
    /// </summary>
    public sealed record SelectionsTreeNode(
            [property: DisplayName("URI")] FeedUri Uri,
            ImplementationVersion? Version,
            string? Path,
            [property: Browsable(false)] SelectionsTreeNode? Parent)
        : INamed<SelectionsTreeNode>
    {
        private string NameBase => (Parent == null)
            ? Uri.ToStringRfc()
            : $"{Parent.NameBase}#{Uri.ToStringRfc()}";

        /// <summary>
        /// The UI path name of this node. Uses a hash (#) as the separator in hierarchical names.
        /// </summary>
        [Browsable(false)]
        public string Name { get => $"{NameBase}#{Version}"; set {} }

        /// <summary>
        /// A prefix used to indicate the indentation level inside the tree structure.
        /// </summary>
        [Browsable(false)]
        private string Prefix => (Parent == null) ? "- " : "  " + Parent.Prefix;

        /// <summary>
        /// Creates string representation with indentation suitable for console output.
        /// </summary>
        public override string ToString()
        {
            string indent = "".PadRight(Prefix.Length);
            if (Version == null) return $"{Prefix}URI: {Uri}\n{indent}{Resources.NoSelectedVersion}";
            else if (Path == null) return $"{Prefix}URI: {Uri}\n{indent}Version: {Version}\n{indent}{Resources.NotCached}";
            else return $"{Prefix}URI: {Uri}\n{indent}Version: {Version}\n{indent}Path: {Path}";
        }

        /// <inheritdoc/>
        public int CompareTo(SelectionsTreeNode? other)
            => string.Compare(Name, other?.Name ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
