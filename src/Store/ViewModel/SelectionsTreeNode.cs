// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Models an <see cref="ImplementationSelection"/> element of a <see cref="Selections"/> document for display in a tree-like UI.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Justification = "Comparison only used for INamed sorting")]
    public sealed class SelectionsTreeNode : INamed<SelectionsTreeNode>, IEquatable<SelectionsTreeNode>
    {
        private string NameBase => (Parent == null)
            ? Uri.ToStringRfc()
            : $"{Parent.NameBase}#{Uri.ToStringRfc()}";

        /// <summary>
        /// The UI path name of this node. Uses a hash (#) as the separator in hierarchical names.
        /// </summary>
        [Browsable(false)]
        [NotNull]
        public string Name { get => $"{NameBase}#{Version}"; set {} }

        /// <summary>
        /// The parent element of this node in the tree structure. <c>null</c> for the root element.
        /// </summary>
        [Browsable(false)]
        [CanBeNull]
        public SelectionsTreeNode Parent { get; }

        /// <summary>
        /// A prefix used to indicate the indentation level inside the tree structure.
        /// </summary>
        [DisplayName(" ")]
        [NotNull]
        public string Prefix => (Parent == null) ? "- " : "  " + Parent.Prefix;

        /// <summary>
        /// The feed URI of the selected implementation.
        /// </summary>
        [DisplayName("URI")]
        [NotNull]
        public FeedUri Uri { get; }

        /// <summary>
        /// The version of the selected implementation. <c>null</c> for a missing selection.
        /// </summary>
        [CanBeNull]
        public ImplementationVersion Version { get; }

        /// <summary>
        /// The local path of the cached implementation. <c>null</c> for an uncached implementation.
        /// </summary>
        [CanBeNull]
        public string Path { get; }

        public SelectionsTreeNode([NotNull] FeedUri uri, [CanBeNull] ImplementationVersion version, [CanBeNull] string path, [CanBeNull] SelectionsTreeNode parent)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            Version = version;
            Path = path;
            Parent = parent;
        }

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

        #region Equality
        /// <inheritdoc/>
        public bool Equals(SelectionsTreeNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Uri == other.Uri && Version == other.Version && Path == other.Path && Equals(Parent, other.Parent);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SelectionsTreeNode node && Equals(node);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Uri, Version, Path, Parent);
        #endregion

        #region Comparison
        /// <inheritdoc/>
        public int CompareTo(SelectionsTreeNode other)
            => string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        #endregion
    }
}
