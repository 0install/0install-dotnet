// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Model
{
    /// <summary>
    /// A linked feed that contains more implementations of this interface. Is treated by the solver as if it were part of the main feed.
    /// </summary>
    /// <seealso cref="Feed.Feeds"/>
    [Description("A linked feed that contains more implementations of this interface. Is treated by the solver as if it were part of the main feed.")]
    [Serializable, XmlRoot("feed", Namespace = Feed.XmlNamespace), XmlType("feed", Namespace = Feed.XmlNamespace)]
    public sealed class FeedReference : TargetBase, ICloneable<FeedReference>, IEquatable<FeedReference>
    {
        /// <summary>
        /// The URL or local path used to locate the feed.
        /// </summary>
        [Description("The URL or local path used to locate the feed.")]
        [XmlIgnore]
        public FeedUri Source { get; set; } = default!;

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="Source"/>
        [XmlAttribute("src"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        // ReSharper disable once ConstantConditionalAccessQualifier
        public string SourceString { get => Source?.ToStringRfc()!; set => Source = new FeedUri(value); }
        #endregion

        #region Normalize
        /// <summary>
        /// Converts legacy elements, sets default values, etc..
        /// </summary>
        /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
        public void Normalize()
            => EnsureAttribute(Source, "src");
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the feed reference in the form "Source (Architecture, Languages)". Not safe for parsing!
        /// </summary>
        public override string ToString() => (Languages.Count == 0)
            ? $"{Source} ({Architecture})"
            : $"{Source} ({Architecture}, {Languages})";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="FeedReference"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="FeedReference"/>.</returns>
        public FeedReference Clone()
        {
            var feedReference = new FeedReference {Source = Source};
            CloneFromTo(this, feedReference);
            return feedReference;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(FeedReference? other)
            => other != null && base.Equals(other) && other.Source == Source;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is FeedReference reference && Equals(reference);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Source);
        #endregion
    }
}
