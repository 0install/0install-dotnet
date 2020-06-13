// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using NanoByte.Common.Collections;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Native
{
    /// <summary>
    /// Base class for <see cref="IPackageManager"/> implementations using template methods.
    /// </summary>
    public abstract class PackageManagerBase : IPackageManager
    {
        /// <summary>
        /// The name of the <see cref="PackageImplementation.Distributions"/> this package manager provides packages for.
        /// </summary>
        protected abstract string DistributionName { get; }

        /// <inheritdoc/>
        public IEnumerable<ExternalImplementation> Query(PackageImplementation package, params string[] distributions)
        {
            #region Sanity checks
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (distributions == null) throw new ArgumentNullException(nameof(distributions));
            #endregion

            if (!distributions.ContainsOrEmpty(DistributionName) || !package.Distributions.ContainsOrEmpty(DistributionName) || string.IsNullOrEmpty(package.Package)) yield break;

            var range = package.Version ?? new VersionRange();
            var implementations = GetImplementations(package.Package).Where(x => range.Match(x.Version));

            foreach (var implementation in implementations)
            {
                CopyValues(from: package, to: implementation);
                yield return implementation;
            }
        }

        /// <inheritdoc/>
        public ExternalImplementation Lookup(ImplementationSelection selection)
        {
            #region Sanity checks
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            #endregion

            try
            {
                var referenceImpl = ExternalImplementation.FromID(selection.ID);

                // Reference implementation from ID does not contain all required information.
                // Therefore, find the original implementation.
                var implementation = GetImplementations(referenceImpl.Package)
                   .First(x => x.Version == referenceImpl.Version
                            && x.Architecture == referenceImpl.Architecture);

                CopyValues(from: selection, to: implementation);
                return implementation;
            }
            #region Error handling
            catch (FormatException)
            {
                throw new ImplementationNotFoundException(string.Format(Resources.UnknownPackageID, selection.ID, DistributionName));
            }
            catch (InvalidOperationException)
            {
                throw new ImplementationNotFoundException(string.Format(Resources.UnknownPackageID, selection.ID, DistributionName));
            }
            #endregion
        }

        /// <summary>
        /// Retrieves a set of specific native implementations for a package name.
        /// </summary>
        /// <param name="packageName">The name of the package to look for.</param>
        protected abstract IEnumerable<ExternalImplementation> GetImplementations(string packageName);

        private static void CopyValues(Element from, ExternalImplementation to)
        {
            to.Commands.AddRange(from.Commands);
            to.Bindings.AddRange(from.Bindings);
        }
    }
}
