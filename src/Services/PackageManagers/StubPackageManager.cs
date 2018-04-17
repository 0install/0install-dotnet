// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Linq;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.PackageManagers
{
    /// <summary>
    /// Stub implementation of <see cref="IPackageManager"/>, used when there is no native package manager support for the current platform.
    /// </summary>
    public class StubPackageManager : IPackageManager
    {
        /// <summary>
        /// Always returns an empty list.
        /// </summary>
        public IEnumerable<ExternalImplementation> Query(PackageImplementation package, params string[] distributions) => Enumerable.Empty<ExternalImplementation>();

        /// <summary>
        /// Always throws <see cref="ImplementationNotFoundException"/>.
        /// </summary>
        public ExternalImplementation Lookup(ImplementationSelection selection) => throw new ImplementationNotFoundException(Resources.NoPackageManagerSupport);
    }
}
