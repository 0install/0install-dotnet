// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

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
        private readonly IImplementationStore _implementationStore;

        /// <summary>
        /// Creates a new executor.
        /// </summary>
        /// <param name="implementationStore">Used to locate the selected <see cref="Implementation"/>s.</param>
        public Executor(IImplementationStore implementationStore)
        {
            _implementationStore = implementationStore ?? throw new ArgumentNullException(nameof(implementationStore));
        }
        #endregion

        /// <inheritdoc/>
        public Process? Start(Selections selections)
            => new EnvironmentBuilder(_implementationStore)
              .Inject(selections)
              .Start();

        /// <inheritdoc/>
        public IEnvironmentBuilder Inject(Selections selections, string? overrideMain = null) => new EnvironmentBuilder(_implementationStore).Inject(selections, overrideMain);
    }
}
