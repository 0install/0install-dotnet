// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Store.Model.Preferences
{
    /// <summary>
    /// Stores user-specific preferences for an <see cref="Implementation"/>.
    /// </summary>
    [XmlType("implementation-preferences", Namespace = Feed.XmlNamespace)]
    public sealed class ImplementationPreferences : XmlUnknown, ICloneable<ImplementationPreferences>, IEquatable<ImplementationPreferences>
    {
        /// <summary>
        /// A unique identifier for the implementation. Corresponds to <see cref="ImplementationBase.ID"/>.
        /// </summary>
        [Description("A unique identifier for the implementation.")]
        [XmlAttribute("id")]
        public string ID { get; set; }

        /// <summary>
        /// A user-specified override for <see cref="Element.Stability"/> specified in the feed.
        /// </summary>
        [Description("A user-specified override for the implementation stability specified in the feed.")]
        [XmlAttribute("user-stability"), DefaultValue(typeof(Stability), "Unset")]
        public Stability UserStability { get; set; } = Stability.Unset;

        /// <summary>
        /// Indicates whether this configuration object stores no information other than the <see cref="ID"/> and is thus superfluous.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public bool IsSuperfluous => (UserStability == Stability.Unset);

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="ImplementationPreferences"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="ImplementationPreferences"/>.</returns>
        public ImplementationPreferences Clone() => new ImplementationPreferences {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, UserStability = UserStability};
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the preferences in the form "ImplementationPreferences: ID". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"ImplementationPreferences: {ID}";
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ImplementationPreferences other)
            => other != null
            && base.Equals(other)
            && ID == other.ID
            && UserStability == other.UserStability;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ImplementationPreferences preferences && Equals(preferences);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), ID, UserStability);
        #endregion
    }
}
