// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Make a chosen <see cref="Implementation"/> available as an executable at runtime.
    /// </summary>
    [XmlType("executable-in-binding", Namespace = Feed.XmlNamespace)]
    public abstract class ExecutableInBinding : Binding
    {
        /// <summary>
        /// The name of the <see cref="Command"/> in the <see cref="Implementation"/> to launch; leave <c>null</c> for <see cref="Model.Command.NameRun"/>.
        /// </summary>
        [Description("The name of the command in the implementation to launch; leave empty for 'run'.")]
        [TypeConverter(typeof(CommandNameConverter))]
        [XmlAttribute("command"), DefaultValue("")]
        public string? Command { get; set; }

        #region Equality
        protected bool Equals(ExecutableInBinding other) => other != null && base.Equals(other) && Command == other.Command;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Command);
        #endregion
    }
}
