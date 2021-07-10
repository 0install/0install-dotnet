// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Builds an implementation archive file.
    /// </summary>
    public interface IArchiveBuilder : IForwardOnlyBuilder, IDisposable
    {}
}
