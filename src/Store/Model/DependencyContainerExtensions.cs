/*
 * Copyright 2010-2017 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Provides extension methods for <see cref="IDependencyContainer"/>.
    /// </summary>
    public static class DependencyContainerExtensions
    {
        /// <summary>
        /// A combination of <see cref="IDependencyContainer.Restrictions"/> and <see cref="IDependencyContainer.Dependencies"/>.
        /// </summary>
        public static IEnumerable<Restriction> GetEffectiveRestrictions(this IDependencyContainer container)
            => container.Restrictions.Concat(container.Dependencies.Cast<Restriction>());
    }
}
