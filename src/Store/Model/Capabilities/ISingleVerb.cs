// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Store.Model.Capabilities
{
    /// <summary>
    /// Interface for capabilities that have a single <see cref="Verb"/>.
    /// </summary>
    public interface ISingleVerb
    {
        /// <summary>
        /// The command to execute when the handler gets called.
        /// </summary>
        [Browsable(false)]
        [XmlElement("verb")]
        Verb Verb { get; set; }
    }
}
