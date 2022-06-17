// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Native;

/// <summary>
/// Combines multiple <see cref="IPackageManager"/>s as a composite.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class CompositePackageManager : IPackageManager
{
    private readonly IPackageManager[] _packageManagers;

    /// <summary>
    /// Creates a new composite package manager with a set of <see cref="IPackageManager"/>s.
    /// </summary>
    /// <param name="packageManagers">A priority-sorted list of <see cref="IPackageManager"/>s. Queried first-to-last.</param>
    public CompositePackageManager(IEnumerable<IPackageManager> packageManagers)
    {
        #region Sanity checks
        if (packageManagers == null) throw new ArgumentNullException(nameof(packageManagers));
        #endregion

        _packageManagers = packageManagers.ToArray();
    }

    /// <inheritdoc/>
    public IEnumerable<ExternalImplementation> Query(PackageImplementation package, params string[] distributions)
        => _packageManagers
          .SelectMany(x => x.Query(package, distributions));

    /// <inheritdoc/>
    public ExternalImplementation? Lookup(ImplementationSelection selection)
        => _packageManagers
          .Select(x => x.Lookup(selection))
          .WhereNotNull()
          .FirstOrDefault();
}
