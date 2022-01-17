// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// An object that contains <see cref="Dependency"/>s and <see cref="Restriction"/>s.
/// </summary>
public interface IDependencyContainer
{
    /// <summary>
    /// A list of interfaces this implementation depends upon.
    /// </summary>
    [XmlElement("requires")]
    List<Dependency> Dependencies { get; }

    /// <summary>
    /// A list of interfaces that are restricted to specific versions when used.
    /// </summary>
    [XmlElement("restricts")]
    List<Restriction> Restrictions { get; }
}
