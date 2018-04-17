// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using JetBrains.Annotations;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Models information about elements in a cache for display in a UI.
    /// </summary>
    public abstract class StoreNode : CacheNode
    {
        /// <summary>The store containing the element.</summary>
        [NotNull]
        protected readonly IStore Store;

        /// <summary>
        /// Creates a new store node.
        /// </summary>
        /// <param name="store">The store containing the element.</param>
        protected StoreNode([NotNull] IStore store)
        {
            Store = store;
        }

        /// <summary>
        /// The file system path of the element.
        /// </summary>
        [Description("The file system path of the element.")]
        [CanBeNull]
        public abstract string Path { get; }
    }
}
