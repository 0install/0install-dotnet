// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Make a chosen <see cref="Implementation"/> available as an executable path in an environment variable.
    /// </summary>
    [Description("Make a chosen implementation available as an executable path in an environment variable.")]
    [Serializable, XmlRoot("executable-in-var", Namespace = Feed.XmlNamespace), XmlType("executable-in-var", Namespace = Feed.XmlNamespace)]
    public sealed class ExecutableInVar : ExecutableInBinding, IEquatable<ExecutableInVar>
    {
        /// <summary>
        /// The name of the environment variable.
        /// </summary>
        [Description("The name of the environment variable.")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "Name = Command". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Name} = {Command ?? Model.Command.NameRun}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="ExecutableInVar"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="ExecutableInVar"/>.</returns>
        public override Binding Clone() => new ExecutableInVar {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Name = Name, Command = Command};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ExecutableInVar? other) => other != null && base.Equals(other) && other.Name == Name;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ExecutableInVar var && Equals(var);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Name);
        #endregion
    }
}
