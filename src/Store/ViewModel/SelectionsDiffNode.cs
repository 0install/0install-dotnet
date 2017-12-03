/*
 * Copyright 2010-2017 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Models an implementation change between two <see cref="Selections"/> documents for display in a UI.
    /// </summary>
    public sealed class SelectionsDiffNode : IEquatable<SelectionsDiffNode>
    {
        /// <summary>
        /// The interface URI of the changed implementation.
        /// </summary>
        [NotNull]
        [DisplayName("URI")]
        public FeedUri Uri { get; }

        /// <summary>
        /// The old version of the implementation; <c>null</c> if the implementation did not exist in the old selection.
        /// </summary>
        [CanBeNull]
        [DisplayName("Old version")]
        public ImplementationVersion OldVersion { get; }

        /// <summary>
        /// The new version of the implementation; <c>null</c> if the implementation does not exist in the new selection.
        /// </summary>
        [CanBeNull]
        [DisplayName("New version")]
        public ImplementationVersion NewVersion { get; }

        /// <summary>
        /// Creates a new selection diff.
        /// </summary>
        /// <param name="uri">The interface URI of the changed implementation.</param>
        /// <param name="oldVersion">The old version of the implementation; <c>null</c> if the implementation did not exist in the old selection.</param>
        /// <param name="newVersion">The new version of the implementation; <c>null</c> if the implementation does not exist in the new selection.</param>
        public SelectionsDiffNode([NotNull] FeedUri uri, [CanBeNull] ImplementationVersion oldVersion = null, [CanBeNull] ImplementationVersion newVersion = null)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            OldVersion = oldVersion;
            NewVersion = newVersion;
        }

        // <inheritdoc/>
        public override string ToString()
        {
            if (OldVersion == null) return $"{Uri}: new -> {NewVersion}";
            else if (NewVersion == null) return $"{Uri}: removed";
            else return $"{Uri}: {OldVersion} -> {NewVersion}";
        }

        #region Equality
        // <inheritdoc/>
        public bool Equals(SelectionsDiffNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Uri == other.Uri && OldVersion == other.OldVersion && NewVersion == other.NewVersion;
        }

        // <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SelectionsDiffNode node && Equals(node);
        }

        // <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Uri.GetHashCode();
                hashCode = (hashCode * 397) ^ (OldVersion != null ? OldVersion.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NewVersion != null ? NewVersion.GetHashCode() : 0);
                return hashCode;
            }
        }
        #endregion
    }
}
