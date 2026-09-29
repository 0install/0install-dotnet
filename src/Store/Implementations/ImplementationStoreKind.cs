// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Implementations;

/// <seealso cref="IImplementationStore.Kind"/>
public enum ImplementationStoreKind
{
    /// <summary>
    /// This store can be written to directly.
    /// </summary>
    ReadWrite,

    /// <summary>
    /// This store cannot be modified.
    /// </summary>
    ReadOnly
}
