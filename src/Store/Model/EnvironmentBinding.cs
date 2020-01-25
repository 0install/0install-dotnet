// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Store.Model
{
    #region Enumerations
    /// <summary>
    /// Controls how <see cref="EnvironmentBinding.Insert"/> or <see cref="EnvironmentBinding.Value"/> is added to a variable.
    /// </summary>
    public enum EnvironmentMode
    {
        /// <summary>The absolute path of the item is prepended to the current value of the variable.</summary>
        [XmlEnum("prepend")]
        Prepend,

        /// <summary>The absolute path of the item is append to the end of the current value of the variable.</summary>
        [XmlEnum("append")]
        Append,

        /// <summary>The old value is overwritten, and the <see cref="EnvironmentBinding.Default"/> attribute is ignored.</summary>
        [XmlEnum("replace")]
        Replace
    }
    #endregion

    /// <summary>
    /// Make a chosen <see cref="Implementation"/> available by setting environment variables.
    /// </summary>
    [Description("Make a chosen implementation available by setting environment variables.")]
    [Serializable, XmlRoot("environment", Namespace = Feed.XmlNamespace), XmlType("environment", Namespace = Feed.XmlNamespace)]
    public sealed class EnvironmentBinding : Binding, IEquatable<EnvironmentBinding>
    {
        /// <summary>
        /// The name of the environment variable.
        /// </summary>
        [Description("The name of the environment variable.")]
        [XmlAttribute("name")]
        [Localizable(false)]
        public string Name { get; set; }

        /// <summary>
        /// A static value to set the variable to.
        /// </summary>
        /// <remarks>If this is set <see cref="Insert"/> must be <c>null</c>.</remarks>
        [Description("A static value to set the variable to. If this is set 'Insert' must be empty.")]
        [XmlAttribute("value")]
        public string? Value { get; set; }

        /// <summary>
        /// The relative path of the item within the implementation to insert into the variable value. Use <c>.</c> to publish the root directory.
        /// </summary>
        /// <remarks>If this is set <see cref="Value"/> must be <c>null</c>.</remarks>
        [Description("The relative path of the item within the implementation to insert into the variable value. Use \".\" to publish the root directory. If this is set 'Value' must be empty.")]
        [XmlAttribute("insert")]
        public string? Insert { get; set; }

        /// <summary>
        /// Controls how the <see cref="Insert"/> or <see cref="Value"/> is added to the variable.
        /// </summary>
        [Description("Controls how 'Insert' or 'Value' is added to the variable.")]
        [XmlAttribute("mode"), DefaultValue(typeof(EnvironmentMode), "Prepend")]
        public EnvironmentMode Mode { get; set; }

        /// <summary>
        /// Overrides the default separator character (":" on POSIX and ";" on Windows).
        /// </summary>
        [Description("Overrides the default separator character (\":\" on POSIX and \";\" on Windows).")]
        [XmlAttribute("separator"), DefaultValue("")]
        public string? Separator { get; set; }

        /// <summary>
        /// If the environment variable is not currently set then this value is used for prepending or appending.
        /// </summary>
        [Description("If the environment variable is not currently set then this value is used for prepending or appending.")]
        [XmlAttribute("default"), DefaultValue("")]
        public string? Default { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "Name = Value (Mode, Default)". Not safe for parsing!
        /// </summary>
        public override string ToString() => string.IsNullOrEmpty(Insert)
            ? $"{Name} = {Value} ({Mode})"
            : $"{Name} = Impl+{Insert} ({Mode})";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="EnvironmentBinding"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="EnvironmentBinding"/>.</returns>
        public override Binding Clone() => new EnvironmentBinding {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Name = Name, Value = Value, Insert = Insert, Mode = Mode, Separator = Separator, Default = Default};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(EnvironmentBinding other)
            => other != null
            && base.Equals(other)
            && other.Name == Name
            && other.Value == Value
            && other.Insert == Insert
            && other.Mode == Mode
            && other.Separator == Separator
            && other.Default == Default;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is EnvironmentBinding binding && Equals(binding);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                Name,
                Value,
                Insert,
                Mode,
                Separator,
                Default);
        #endregion
    }
}
