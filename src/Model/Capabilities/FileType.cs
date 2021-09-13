// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// An application's ability to open a certain file type.
    /// </summary>
    [Description("An application's ability to open a certain file type.")]
    [Serializable, XmlRoot("file-type", Namespace = CapabilityList.XmlNamespace), XmlType("file-type", Namespace = CapabilityList.XmlNamespace)]
    public sealed class FileType : VerbCapability, IEquatable<FileType>
    {
        /// <summary>
        /// A list of all file extensions associated with this file type.
        /// </summary>
        [Browsable(false)]
        [XmlElement("extension")]
        public List<FileTypeExtension> Extensions { get; } = new();

        /// <inheritdoc/>
        [XmlIgnore]
        public override IEnumerable<string> ConflictIDs => new[] {"progid:" + ID};

        #region Normalize
        /// <inheritdoc/>
        public override void Normalize()
        {
            base.Normalize();
            foreach (var extension in Extensions) extension.Normalize();
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the capability in the form "ID". Not safe for parsing!
        /// </summary>
        public override string ToString()
            => $"{ID}";
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override Capability Clone()
        {
            var capability = new FileType {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, ExplicitOnly = ExplicitOnly};
            capability.Descriptions.AddRange(Descriptions.CloneElements());
            capability.Icons.AddRange(Icons);
            capability.Verbs.AddRange(Verbs.CloneElements());
            capability.Extensions.AddRange(Extensions);
            return capability;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(FileType? other)
            => other != null && base.Equals(other) && Extensions.SequencedEquals(other.Extensions);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is FileType type && Equals(type);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Extensions.GetSequencedHashCode());
        #endregion
    }
}
