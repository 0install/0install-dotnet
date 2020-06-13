// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Bindings specify how the chosen implementation is made known to the running program.
    /// </summary>
    /// <remarks>
    /// Bindings can appear in <see cref="Dependency"/>s, in which case they tell a component how to find its dependency,
    /// or in <see cref="Element"/>, where they tell a component how to find itself.
    /// </remarks>
    [XmlType("binding-base", Namespace = Feed.XmlNamespace)]
    public abstract class Binding : FeedElement, ICloneable<Binding>
    {
        /// <summary>
        /// Creates a deep copy of this <see cref="Binding"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Binding"/>.</returns>
        public abstract Binding Clone();
    }
}
