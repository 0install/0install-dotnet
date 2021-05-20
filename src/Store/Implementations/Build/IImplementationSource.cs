// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Tasks;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Provides content for building an implementation.
    /// </summary>
    /// <see cref="IImplementationStore.Add"/>
    public interface IImplementationSource
    {
        /// <summary>
        /// Returns a task that applies the source to build the implementation.
        /// </summary>
        /// <param name="targetPath">The directory to build the implementation in.</param>
        /// <returns>A task. May be <see cref="IDisposable"/>.</returns>
        ITask GetApplyTask(string targetPath);
    }
}
