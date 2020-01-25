// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// A single command-line arguments to be passed to an executable.
    /// </summary>
    [Description("A single command-line arguments to be passed to an executable.")]
    [Serializable, XmlRoot("arg", Namespace = Feed.XmlNamespace), XmlType("arg", Namespace = Feed.XmlNamespace)]
    public class Arg : ArgBase, ICloneable<Arg>, IEquatable<Arg>
    {
        /// <summary>
        /// A single command-line arguments to be passed to an executable.
        /// Will be automatically escaped to allow proper concatenation of multiple arguments containing spaces.
        /// </summary>
        [Description("A single command-line arguments to be passed to an executable.\r\nWill be automatically escaped to allow proper concatenation of multiple arguments containing spaces.")]
        [XmlText]
        public string Value { get; set; }

        #region Normalize
        /// <inheritdoc/>
        public override void Normalize() => EnsureNotNull(Value, xmlAttribute: "value", xmlTag: "arg");
        #endregion

        #region Conversion
        /// <summary>
        /// Convenience cast for turning strings into plain <see cref="Arg"/>s.
        /// </summary>
        public static implicit operator Arg(string value) => new Arg {Value = value};

        /// <summary>
        /// Returns <see cref="Value"/> directly. Safe for parsing!
        /// </summary>
        public override string ToString() => Value ?? "(empty)";
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Arg other) => other != null && (base.Equals(other) && other.Value == Value);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Arg arg && Equals(arg);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Value);
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Arg"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Arg"/>.</returns>
        Arg ICloneable<Arg>.Clone() => new Arg {Value = Value};

        /// <summary>
        /// Creates a deep copy of this <see cref="Arg"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Arg"/>.</returns>
        public override ArgBase Clone() => ((ICloneable<Arg>)this).Clone();
        #endregion
    }
}
