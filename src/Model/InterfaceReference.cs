// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Model
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
        public FeedUri Target { get; set; } = default!;

        #region XML serialization
        /// <summary>Used for XML serialization and PropertyGrid.</summary>
        /// <seealso cref="Target"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [DisplayName(@"Target"), Description("The URI used to locate the interface.")]
        [XmlAttribute("interface"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        // ReSharper disable once ConstantConditionalAccessQualifier
        public string TargetString { get => Target?.ToStringRfc()!; set => Target = new(value); }
        #endregion

        #region Normalize
        /// <summary>
        /// Converts legacy elements, sets default values, etc..
        /// </summary>
        /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
        public void Normalize()
            => EnsureAttribute(Target, "target");
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
        public InterfaceReference Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Target = Target};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(InterfaceReference? other)
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
