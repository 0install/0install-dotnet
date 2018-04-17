// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Executors
{
    /// <summary>
    /// Executes a <see cref="Selections"/> document as a program using dependency injection.
    /// </summary>
    public class Executor : IExecutor
    {
        #region Dependencies
        /// <summary>
        /// Used to locate the selected <see cref="Implementation"/>s.
        /// </summary>
        private readonly IStore _store;

        /// <summary>
        /// Creates a new executor.
        /// </summary>
        /// <param name="store">Used to locate the selected <see cref="Implementation"/>s.</param>
        public Executor([NotNull] IStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }
        #endregion

        /// <inheritdoc/>
        public Process Start(Selections selections) => new EnvironmentBuilder(_store).Inject(selections).Start();

        /// <inheritdoc/>
        public IEnvironmentBuilder Inject(Selections selections, string overrideMain = null) => new EnvironmentBuilder(_store).Inject(selections, overrideMain);
    }
}
