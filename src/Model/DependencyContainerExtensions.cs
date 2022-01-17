// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Provides extension methods for <see cref="IDependencyContainer"/>.
/// </summary>
public static class DependencyContainerExtensions
{
    /// <summary>
    /// A combination of <see cref="IDependencyContainer.Restrictions"/> and <see cref="IDependencyContainer.Dependencies"/>.
    /// </summary>
    public static IEnumerable<Restriction> GetEffectiveRestrictions(this IDependencyContainer container)
        => container.Restrictions.Concat(container.Dependencies);
}
