// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Executes a <see cref="Selections"/> document as a program using dependency injection.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class Executor : IExecutor
{
    private readonly IImplementationStore _implementationStore;
    private readonly IExecutionStrategy? _executionStrategy;

    /// <summary>
    /// Creates a new executor.
    /// </summary>
    /// <param name="implementationStore">The implementation store to use.</param>
    /// <param name="executionStrategy">The execution strategy to use. If null, uses native process execution.</param>
    public Executor(IImplementationStore implementationStore, IExecutionStrategy? executionStrategy = null)
    {
        _implementationStore = implementationStore;
        _executionStrategy = executionStrategy;
    }

    /// <inheritdoc/>
    public Process? Start(Selections selections)
        => new EnvironmentBuilder(_implementationStore, _executionStrategy)
          .Inject(selections)
          .Start();

    /// <inheritdoc/>
    public IEnvironmentBuilder Inject(Selections selections, string? overrideMain = null)
        => new EnvironmentBuilder(_implementationStore, _executionStrategy)
           .Inject(selections, overrideMain);
}
