// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Native;

/// <summary>
/// Combines multiple <see cref="IPackageManager"/>s as a composite.
/// </summary>
/// <param name="packageManagers">A priority-sorted list of <see cref="IPackageManager"/>s. Queried first-to-last.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class CompositePackageManager(params IEnumerable<IPackageManager> packageManagers) : IPackageManager
{
    private readonly List<IPackageManager> _packageManagers = packageManagers.ToList();

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
