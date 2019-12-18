// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Abstract base class for XML serializable classes that are part of the Zero Install feed model.
    /// </summary>
    /// <remarks>Does not include <see cref="Store.Model.Capabilities"/>.</remarks>
    public abstract class FeedElement : XmlUnknown
    {
        /// <summary>
        /// Only process this element if the current Zero Install version matches the range.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore, CanBeNull]
        public VersionRange IfZeroInstallVersion { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="IfZeroInstallVersion"/>
        [XmlAttribute("if-0install-version"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string IfZeroInstallVersionString { get => IfZeroInstallVersion?.ToString(); set => IfZeroInstallVersion = string.IsNullOrEmpty(value) ? null : new VersionRange(value); }
        #endregion

        #region Filter
        /// <summary>
        /// Checks whether an element passes the specified <see cref="IfZeroInstallVersion"/> restriction, if any.
        /// </summary>
        protected internal static bool FilterMismatch<T>(T element)
            where T : FeedElement
        {
            if (element == null) return false;

            return element.IfZeroInstallVersion != null && !element.IfZeroInstallVersion.Match(ImplementationVersion.ZeroInstall);
        }

        /// <summary>
        /// Checks whether an element passes the specified <see cref="IfZeroInstallVersion"/> restriction, if any.
        /// </summary>
        protected static bool FilterMismatch(IRecipeStep step) => FilterMismatch(step as FeedElement);
        #endregion

        #region Equality
        protected bool Equals(FeedElement other) => other != null && base.Equals(other) && IfZeroInstallVersion == other.IfZeroInstallVersion;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), IfZeroInstallVersion);
        #endregion
    }
}
