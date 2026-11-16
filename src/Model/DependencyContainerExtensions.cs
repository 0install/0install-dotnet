// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Provides extension methods for <see cref="IDependencyContainer"/>.
/// </summary>
public static class DependencyContainerExtensions
{
    extension(IDependencyContainer container)
    {
        /// <summary>
        /// A combination of <see cref="IDependencyContainer.Restrictions"/> and <see cref="IDependencyContainer.Dependencies"/>.
        /// </summary>
        public IEnumerable<Restriction> GetEffectiveRestrictions()
            => [..container.Restrictions, ..container.Dependencies];
    }
}
