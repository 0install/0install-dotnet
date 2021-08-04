// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model
{
    /// <summary>
    /// All attributes of a group are inherited by any child <see cref="Group"/>s and <see cref="Implementation"/>s as defaults, but can be overridden there.
    /// All <see cref="Dependency"/>s and <see cref="Binding"/>s are inherited (sub-groups may add more <see cref="Dependency"/>s and <see cref="Binding"/>s to the list, but cannot remove any).
    /// </summary>
    /// <seealso cref="Feed.Elements"/>
    [Description("All attributes of a group are inherited by any child Groups and Implementations as defaults, but can be overridden there.\r\nAll Dependencies and Bindings are inherited (sub-groups may add more Dependencies and Bindings to the list, but cannot remove any).")]
    [Serializable, XmlRoot("group", Namespace = Feed.XmlNamespace), XmlType("group", Namespace = Feed.XmlNamespace)]
    public sealed class Group : Element, IElementContainer, IEquatable<Group>
    {
        /// <inheritdoc/>
        internal override IEnumerable<Implementation> Implementations => Elements.SelectMany(x => x.Implementations);

        /// <summary>
        /// A list of <see cref="Group"/>s and <see cref="Implementation"/>s contained within this group.
        /// </summary>
        [Browsable(false)]
        [XmlElement(typeof(Implementation)), XmlElement(typeof(PackageImplementation)), XmlElement(typeof(Group))]
        public List<Element> Elements { get; } = new();

        #region Normalize
        /// <inheritdoc/>
        public override void Normalize(FeedUri? feedUri = null)
        {
            // Apply if-0install-version filter
            Elements.RemoveAll(FilterMismatch);

            var collapsedElements = new List<Element>();

            foreach (var element in Elements)
            {
                element.InheritFrom(this);

                // Flatten structure in sub-groups, set missing default values in implementations
                element.Normalize(feedUri);

                if (element is Group group)
                {
                    // Move implementations out of sub-groups
                    collapsedElements.AddRange(group.Elements);
                }
                else collapsedElements.Add(element);
            }
            Elements.Clear();
            Elements.AddRange(collapsedElements);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Group"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Group"/>.</returns>
        public Group CloneGroup()
        {
            var group = new Group();
            CloneFromTo(this, group);
            foreach (var element in Elements)
                group.Elements.Add(element.Clone());

            return group;
        }

        /// <inheritdoc/>
        public override Element Clone() => CloneGroup();
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the group in the form "Comma-separated list of set values". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();
            if (Architecture != default) parts.Add(Architecture.ToString());
            if (Version != null) parts.Add(Version.ToString());
            if (Released != default) parts.Add(Released.ToString("d", CultureInfo.InvariantCulture));
            if (ReleasedVerbatim != null) parts.Add(ReleasedVerbatim);
            if (Stability != default) parts.Add(Stability.ToString());
            if (!string.IsNullOrEmpty(License)) parts.Add(License);
            if (!string.IsNullOrEmpty(Main)) parts.Add(Main);
            return StringUtils.Join(", ", parts);
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Group? other) => other != null && base.Equals(other) && Elements.SequencedEquals(other.Elements);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Group group && Equals(group);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Elements.GetSequencedHashCode());
        #endregion
    }
}
