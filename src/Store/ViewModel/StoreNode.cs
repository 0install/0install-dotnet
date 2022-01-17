// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about elements in a cache for display in a UI.
/// </summary>
public abstract class StoreNode : CacheNode
{
    /// <summary>The store containing the element.</summary>
    protected readonly IImplementationStore ImplementationStore;

    /// <summary>
    /// Creates a new store node.
    /// </summary>
    /// <param name="implementationStore">The store containing the element.</param>
    protected StoreNode(IImplementationStore implementationStore)
    {
        ImplementationStore = implementationStore;
    }

    /// <summary>
    /// The file system path of the element.
    /// </summary>
    [Description("The file system path of the element.")]
    public abstract string? Path { get; }
}
