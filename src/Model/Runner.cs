// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common.Collections;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model
{
    /// <summary>
    /// A special kind of dependency: the program that is used to run this one. For example, a Python program might specify Python as its runner.
    /// </summary>
    /// <seealso cref="Model.Command.Runner"/>
    [Description("A special kind of dependency: the program that is used to run this one. For example, a Python program might specify Python as its runner.")]
    [Serializable, XmlRoot("runner", Namespace = Feed.XmlNamespace), XmlType("runner", Namespace = Feed.XmlNamespace)]
    public class Runner : Dependency, IArgBaseContainer, IEquatable<Runner>
    {
        /// <summary>
        /// The name of the command in the <see cref="Restriction.InterfaceUri"/> to use; leave <c>null</c> for <see cref="Model.Command.NameRun"/>.
        /// </summary>
        [Description("The name of the command in the interface to use; leave empty for 'run'.")]
        [TypeConverter(typeof(CommandNameConverter))]
        [XmlAttribute("command"), DefaultValue("")]
        public string? Command { get; set; }

        /// <summary>
        /// A list of command-line arguments to be passed to the runner before the path of the implementation.
        /// </summary>
        [Browsable(false)]
        [XmlElement(typeof(Arg)), XmlElement(typeof(ForEachArgs))]
        public List<ArgBase> Arguments { get; } = new();

        #region Normalize
        protected override string XmlTagName => "runner";

        /// <inheritdoc/>
        public override void Normalize()
        {
            base.Normalize();

            foreach (var argument in Arguments) argument.Normalize();
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the runner in the form "Interface (Command)". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{InterfaceUri} ({Command ?? Model.Command.NameRun})";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Runner"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Runner"/>.</returns>
        public Runner CloneRunner()
        {
            var runner = new Runner {InterfaceUri = InterfaceUri, Use = Use, Command = Command, Versions = Versions};
            runner.Bindings.AddRange(Bindings.CloneElements());
            runner.Constraints.AddRange(Constraints.CloneElements());
            runner.Arguments.AddRange(Arguments.CloneElements());
            return runner;
        }

        /// <summary>
        /// Creates a deep copy of this <see cref="Runner"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Runner"/>.</returns>
        public override Restriction Clone() => CloneRunner();
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Runner? other)
            => other != null
            && base.Equals(other)
            && Command == other.Command
            && Arguments.SequencedEquals(other.Arguments);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(Runner) && Equals((Runner)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                Command,
                Arguments.GetSequencedHashCode());
        #endregion
    }
}
