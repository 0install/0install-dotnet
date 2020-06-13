// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Model
{
    /// <summary>
    /// An object that contains <see cref="Icons"/>s.
    /// </summary>
    public interface IIconContainer
    {
        /// <summary>
        /// Zero or more icons.
        /// </summary>
        [Browsable(false)]
        [XmlElement("icon")]
        List<Icon> Icons { get; }
    }
}
