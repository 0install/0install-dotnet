// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;

namespace ZeroInstall.Store.Model.Capabilities
{
    /// <summary>
    /// Abstract base class for capabilities that can have multiple <see cref="Icon"/>s and descriptions.
    /// </summary>
    [Serializable, XmlType("icon-capability", Namespace = CapabilityList.XmlNamespace)]
    public abstract class IconCapability : DefaultCapability, IIconContainer, IDescriptionContainer
    {
        /// <inheritdoc/>
        [Browsable(false)]
        [XmlElement("description")]
        public LocalizableStringCollection Descriptions { get; } = new LocalizableStringCollection();

        /// <summary>
        /// Zero or more icons to represent the capability. Used for things like file icons.
        /// </summary>
        [Browsable(false)]
        [XmlElement("icon", Namespace = Feed.XmlNamespace), NotNull]
        public List<Icon> Icons { get; } = new List<Icon>();

        /// <summary>
        /// Returns the first icon with a specific MIME type.
        /// </summary>
        /// <param name="mimeType">The <see cref="Icon.MimeType"/> to try to find. Will only return exact matches.</param>
        /// <returns>The best matching icon that was found or <c>null</c> if no matching icon was found.</returns>
        [CanBeNull]
        public Icon GetIcon([NotNull] string mimeType)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            #endregion

            return Icons.FirstOrDefault(icon => StringUtils.EqualsIgnoreCase(icon.MimeType, mimeType) && icon.Href != null);
        }

        #region Equality
        protected bool Equals(IconCapability other) => other != null && base.Equals(other) && Descriptions.SequencedEquals(other.Descriptions) && Icons.SequencedEquals(other.Icons);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ Descriptions.GetSequencedHashCode();
                result = (result * 397) ^ Icons.GetSequencedHashCode();
                return result;
            }
        }
        #endregion
    }
}
