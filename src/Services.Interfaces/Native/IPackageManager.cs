// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using JetBrains.Annotations;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Native
{
    /// <summary>
    /// Handles packages provided by the operating system's native package managers rather than Zero Install itself.
    /// </summary>
    /// <seealso cref="PackageImplementation"/>
    /// <seealso cref="ExternalImplementation"/>
    /// <seealso cref="ExternalRetrievalMethod"/>
    public interface IPackageManager
    {
        /// <summary>
        /// Queries the package manager for all <see cref="ExternalImplementation"/>s that match a specific <see cref="PackageImplementation"/> definition.
        /// </summary>
        /// <param name="package">The definition of the package to look for.</param>
        /// <param name="distributions">Specifies the distributions to check for matching packages. Leave empty to check in all available distributions.</param>
        [NotNull, ItemNotNull]
        IEnumerable<ExternalImplementation> Query([NotNull] PackageImplementation package, [NotNull, ItemNotNull] params string[] distributions);

        /// <summary>
        /// Looks up the specific <see cref="ExternalImplementation"/> an <see cref="ImplementationSelection"/> was based on.
        /// </summary>
        /// <param name="selection">The implementation selection to look up.</param>
        /// <exception cref="ImplementationNotFoundException"><paramref name="selection"/> does not refer to a package known to this package manager.</exception>
        [NotNull]
        ExternalImplementation Lookup([NotNull] ImplementationSelection selection);
    }
}
