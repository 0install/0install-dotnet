// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Zero Install will not know how to run a program using generic bindings itself, but it will include them in any selections documents it creates, which can then be executed by your custom code.
    /// </summary>
    [Description("Zero Install will not know how to run a program using generic bindings itself, but it will include them in any selections documents it creates, which can then be executed by your custom code.")]
    [Serializable, XmlRoot("binding", Namespace = Feed.XmlNamespace), XmlType("binding", Namespace = Feed.XmlNamespace)]
    public sealed class GenericBinding : ExecutableInBinding, IEquatable<GenericBinding>
    {
        /// <summary>
        /// If your binding needs a path within the selected implementation, it is suggested that the path attribute be used for this. Other attributes and child elements should be namespaced to avoid collisions.
        /// </summary>
        [Description("If your binding needs a path within the selected implementation, it is suggested that the path attribute be used for this. Other attributes and child elements should be namespaced to avoid collisions. ")]
        [XmlAttribute("path")]
        public string Path { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "Path = Command". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Path} = {Command}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="GenericBinding"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="GenericBinding"/>.</returns>
        public override Binding Clone()
            => new GenericBinding {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Path = Path, Command = Command};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(GenericBinding other)
            => other != null && base.Equals(other) && other.Path == Path;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is GenericBinding binding && Equals(binding);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Path);
        #endregion
    }
}
