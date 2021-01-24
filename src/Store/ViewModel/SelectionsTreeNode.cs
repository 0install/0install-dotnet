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
        : INamed
    {
        /// <summary>
        /// The full name of the node used for tree hierarchies.
        /// </summary>
        [Browsable(false)]
        public string Name
        {
            get
            {
                string name = $"{Uri.ToStringRfc()} ({(Version?.ToString() ?? Resources.NoSelectedVersion)})";
                return Parent == null
                    ? name
                    : Parent.Name + Named.TreeSeparator + name;
            }
            set => throw new NotSupportedException();
        }

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
    }
}
