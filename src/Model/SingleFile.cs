// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Retrieves an implementation by downloading a single file.
    /// </summary>
    [Description("Retrieves an implementation by downloading a single file.")]
    [Serializable, XmlRoot("file", Namespace = Feed.XmlNamespace), XmlType("file", Namespace = Feed.XmlNamespace)]
    public sealed class SingleFile : DownloadRetrievalMethod, IEquatable<SingleFile>
    {
        /// <summary>
        /// The file's target path relative to the implementation root as a Unix-style path.
        /// </summary>
        [Description("The file's target path relative to the implementation root as a Unix-style path.")]
        [XmlAttribute("dest")]
        public string? Destination { get; set; }

        /// <summary>
        /// Set this to <c>true</c> to mark the file as executable.
        /// </summary>
        [Description("Set this to true to mark the file as executable.")]
        [XmlAttribute("executable"), DefaultValue(false)]
        public bool Executable { get; set; }

        #region Normalize
        protected override string XmlTagName => "file";
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the file in the form "Location (Size) => Destination". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Href} ({Size}) => {Destination}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="SingleFile"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="SingleFile"/>.</returns>
        public override RetrievalMethod Clone() => new SingleFile {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Href = Href, Size = Size, Destination = Destination, Executable = Executable};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(SingleFile other) => other != null && base.Equals(other) && other.Destination == Destination && other.Executable == Executable;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is SingleFile file && Equals(file);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Destination, Executable);
        #endregion
    }
}
