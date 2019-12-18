// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Switches the working directory of a process on startup to a location within an implementation.
    /// Useful for supporting legacy Windows applications which do not properly locate their installation directory.
    /// </summary>
    /// <seealso cref="Command.WorkingDir"/>
    [Description("Switches the working directory of a process on startup to a location within an implementation.\r\nUseful for supporting legacy Windows applications which do not properly locate their installation directory.")]
    [Serializable, XmlRoot("working-dir", Namespace = Feed.XmlNamespace), XmlType("working-dir", Namespace = Feed.XmlNamespace)]
    public sealed class WorkingDir : FeedElement, ICloneable<WorkingDir>, IEquatable<WorkingDir>
    {
        /// <summary>
        /// The relative path of the directory in the implementation to set as the working directory. Defaults to use the root of the implementation if unset.
        /// </summary>
        [Description("The relative path of the directory in the implementation to set as the working directory. Defaults to use the root of the implementation if unset.")]
        [XmlAttribute("src"), DefaultValue("")]
        [CanBeNull]
        public string Source { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "Source". Not safe for parsing!
        /// </summary>
        public override string ToString() => Source ?? "(unset)";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="WorkingDir"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="WorkingDir"/>.</returns>
        public WorkingDir Clone() => new WorkingDir {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Source = Source};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(WorkingDir other)
            => other != null && base.Equals(other) && other.Source == Source;

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is WorkingDir dir && Equals(dir);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Source);
        #endregion
    }
}
