// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Make a chosen <see cref="Implementation"/> available as an executable in the search PATH.
    /// </summary>
    [Description("Make a chosen implementation available as an executable in the search PATH.")]
    [Serializable, XmlRoot("executable-in-path", Namespace = Feed.XmlNamespace), XmlType("executable-in-path", Namespace = Feed.XmlNamespace)]
    public sealed class ExecutableInPath : ExecutableInBinding, IEquatable<ExecutableInPath>
    {
        /// <summary>
        /// The name of the executable (without file extensions).
        /// </summary>
        [Description("The name of the executable (without file extensions).")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the binding in the form " Name = Command". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Name} = {Command ?? Model.Command.NameRun}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="ExecutableInPath"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="ExecutableInPath"/>.</returns>
        public override Binding Clone() => new ExecutableInPath {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Name = Name, Command = Command};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ExecutableInPath other) => other != null && (base.Equals(other) && other.Name == Name);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ExecutableInPath path && Equals(path);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Name);
        #endregion
    }
}
