// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Xml.Serialization;

namespace ZeroInstall.Model
{
    /// <summary>
    /// An object that contains <see cref="ArgBase"/>s.
    /// </summary>
    public interface IArgBaseContainer
    {
        /// <summary>
        /// A list of command-line arguments to be passed to an executable.
        /// </summary>
        [XmlElement(typeof(Arg)), XmlElement(typeof(ForEachArgs))]
        List<ArgBase> Arguments { get; }
    }
}
