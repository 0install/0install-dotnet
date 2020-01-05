// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// A reference to an interface URI, e.g. for specifying which interface this feed implements or by which interface it is replaced.
    /// </summary>
    /// <seealso cref="Feed.FeedFor"/>
    /// <seealso cref="Feed.ReplacedBy"/>
    [Description("A reference to an interface URI, e.g. for specifying which interface this feed implements or by which interface it is replaced.")]
    [Serializable, XmlRoot("feed-for", Namespace = Feed.XmlNamespace), XmlType("feed-for", Namespace = Feed.XmlNamespace)]
    public sealed class InterfaceReference : FeedElement, ICloneable<InterfaceReference>, IEquatable<InterfaceReference>
    {
        /// <summary>
        /// The URI used to locate the interface.
        /// </summary>
        [XmlIgnore, Browsable(false)]
        public FeedUri Target { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization and PropertyGrid.</summary>
        /// <seealso cref="Target"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [DisplayName(@"Target"), Description("The URI used to locate the interface.")]
        [XmlAttribute("interface"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string? TargetString { get => Target?.ToStringRfc(); set => Target = (string.IsNullOrEmpty(value) ? null : new FeedUri(value)); }
        #endregion

        #region Normalize
        /// <summary>
        /// Performs sanity checks.
        /// </summary>
        /// <exception cref="InvalidDataException">One or more required fields are not set.</exception>
        /// <remarks>This method should be called to prepare a <see cref="Feed"/> for solver processing. Do not call it if you plan on serializing the feed again since it may loose some of its structure.</remarks>
        public void Normalize() => EnsureNotNull(Target, xmlAttribute: "interface", xmlTag: "feed-for");
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the interface reference in the form "Target". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Target}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="InterfaceReference"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="InterfaceReference"/>.</returns>
        public InterfaceReference Clone() => new InterfaceReference {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Target = Target};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(InterfaceReference other)
            => other != null && base.Equals(other) && other.Target == Target;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is InterfaceReference reference && Equals(reference);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), TargetString);
        #endregion
    }
}
