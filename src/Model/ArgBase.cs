// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Common base class for <see cref="Arg"/> and <see cref="ForEachArgs"/>.
    /// </summary>
    [TypeConverter(typeof(ArgBaseConverter))]
    [XmlType("arg-base", Namespace = Feed.XmlNamespace)]
    public abstract class ArgBase : FeedElement, ICloneable<ArgBase>
    {
        /// <summary>
        /// Converts legacy elements, sets default values, etc..
        /// </summary>
        /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
        public abstract void Normalize();

        /// <summary>
        /// Creates a deep copy of this <see cref="ArgBase"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="ArgBase"/>.</returns>
        public abstract ArgBase Clone();

        /// <summary>
        /// Convenience cast for turning strings into plain <see cref="Arg"/>s.
        /// </summary>
        public static implicit operator ArgBase(string value) => new Arg {Value = value};
    }
}
