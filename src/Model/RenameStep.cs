// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Renames or moves a file or directory. It is an error if the source or destination are outside the implementation.
    /// </summary>
    [Description("Renames or moves a file or directory. It is an error if the source or destination are outside the implementation.")]
    [Serializable, XmlRoot("rename", Namespace = Feed.XmlNamespace), XmlType("rename", Namespace = Feed.XmlNamespace)]
    public sealed class RenameStep : FeedElement, IRecipeStep, IEquatable<RenameStep>
    {
        /// <summary>
        /// The source file or directory relative to the implementation root as a Unix-style path.
        /// </summary>
        [Description("The source file or directory relative to the implementation root as a Unix-style path.")]
        [XmlAttribute("source"), DefaultValue("")]
        public string? Source { get; set; }

        /// <summary>
        /// The destination file or directory relative to the implementation root as a Unix-style path.
        /// </summary>
        [Description("The destination file or directory relative to the implementation root as a Unix-style path.")]
        [XmlAttribute("dest"), DefaultValue("")]
        public string? Destination { get; set; }

        #region Normalize
        /// <inheritdoc/>
        public void Normalize(FeedUri? feedUri = null) {}
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the rename step in the form "Source => Destination". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Source} => {Destination}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="RenameStep"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="RenameStep"/>.</returns>
        public IRecipeStep Clone() => new RenameStep {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Source = Source, Destination = Destination};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(RenameStep? other) => other != null && base.Equals(other) && other.Source == Source && other.Destination == Destination;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is RenameStep step && Equals(step);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Source, Destination);
        #endregion
    }
}
