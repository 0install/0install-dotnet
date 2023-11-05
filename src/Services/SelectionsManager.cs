// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Services;

/// <summary>
/// Provides methods for filtering <see cref="Selections"/>.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class SelectionsManager(IFeedManager feedManager, IImplementationStore implementationStore, IPackageManager packageManager) : ISelectionsManager
{
    /// <inheritdoc/>
    public IEnumerable<ImplementationSelection> GetUncached(IEnumerable<ImplementationSelection> selections)
    {
        #region Sanity checks
        if (selections == null) throw new ArgumentNullException(nameof(selections));
        #endregion

        foreach (var implementation in selections)
        {
            // Local paths are considered to be always cached
            if (!string.IsNullOrEmpty(implementation.LocalPath)) continue;

            if (implementation.ID.StartsWith(ExternalImplementation.PackagePrefix))
            {
                if (!File.Exists(implementation.QuickTestFile) && packageManager.Lookup(implementation) is not {IsInstalled: true})
                    yield return implementation;
            }
            else
            {
                if (!implementationStore.Contains(implementation.ManifestDigest))
                    yield return implementation;
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<Implementation> GetImplementations(IEnumerable<ImplementationSelection> selections)
    {
        #region Sanity checks
        if (selections == null) throw new ArgumentNullException(nameof(selections));
        #endregion

        foreach (var selection in selections)
        {
            yield return selection.ID.StartsWith(ExternalImplementation.PackagePrefix)
                ? packageManager.Lookup(selection) ?? throw new ImplementationNotFoundException(string.Format(Resources.UnknownPackageID, selection.ID, "native"))
                : feedManager[selection.FromFeed ?? selection.InterfaceUri][selection.ID].CloneImplementation();
        }
    }

    /// <inheritdoc/>
    public NamedCollection<SelectionsTreeNode> GetTree(Selections selections)
    {
        #region Sanity checks
        if (selections == null) throw new ArgumentNullException(nameof(selections));
        #endregion

        var visited = new HashSet<FeedUri>();
        var result = new NamedCollection<SelectionsTreeNode>();

        ImplementationSelection? TryGetImplementation(IInterfaceUri target)
        {
            try
            {
                return selections[target.InterfaceUri];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        void AddNodes(IInterfaceUri target, SelectionsTreeNode? parent)
        {
            // Prevent infinite recursion
            if (visited.Contains(target.InterfaceUri)) return;
            visited.Add(target.InterfaceUri);

            var implementation = TryGetImplementation(target);

            var node = new SelectionsTreeNode(
                target.InterfaceUri,
                implementation?.Version,
                implementation?.To(TryGetPath),
                parent);
            result.Add(node);
            if (implementation == null) return;

            // Recurse into regular dependencies
            foreach (var dependency in implementation.Dependencies)
                AddNodes(dependency, parent: node);

            foreach (var command in implementation.Commands)
            {
                // Recurse into command dependencies
                foreach (var dependency in command.Dependencies)
                    AddNodes(dependency, parent: node);

                // Recurse into runner dependency
                if (command.Runner != null)
                    AddNodes(command.Runner, parent: node);
            }
        }

        AddNodes(selections, parent: null);
        return result;
    }

    private string? TryGetPath(ImplementationBase implementation)
    {
        if (implementation.LocalPath != null) return implementation.LocalPath;
        if (implementation.ID.StartsWith(ExternalImplementation.PackagePrefix)) return $"({implementation.ID})";

        try
        {
            return implementationStore.GetPath(implementation.ManifestDigest);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn(ex);
            return null;
        }
        #endregion
    }

    /// <inheritdoc/>
    public IEnumerable<SelectionsDiffNode> GetDiff(Selections oldSelections, Selections newSelections)
    {
        #region Sanity checks
        if (oldSelections == null) throw new ArgumentNullException(nameof(oldSelections));
        if (newSelections == null) throw new ArgumentNullException(nameof(newSelections));
        #endregion

        foreach (var newImplementation in newSelections.Implementations)
        {
            var interfaceUri = newImplementation.InterfaceUri;
            if (!oldSelections.ContainsImplementation(interfaceUri))
            { // Implementation added
                yield return new(interfaceUri, newVersion: newImplementation.Version);
            }
        }

        foreach (var oldImplementation in oldSelections.Implementations)
        {
            var interfaceUri = oldImplementation.InterfaceUri;

            var newImplementation = newSelections.GetImplementation(interfaceUri);
            if (newImplementation == null)
            { // Implementation removed
                yield return new(interfaceUri, oldVersion: oldImplementation.Version);
            }
            else if (oldImplementation.Version != newImplementation.Version)
            { // Implementation updated
                yield return new(interfaceUri, oldVersion: oldImplementation.Version, newVersion: newImplementation.Version);
            }
        }
    }
}
