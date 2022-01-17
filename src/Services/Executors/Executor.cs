// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Executes a <see cref="Selections"/> document as a program using dependency injection.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public partial class Executor : IExecutor
{
    /// <summary>
    /// Used to locate the selected <see cref="Implementation"/>s.
    /// </summary>
    private readonly IImplementationStore _implementationStore;

    /// <inheritdoc/>
    public Process? Start(Selections selections)
        => new EnvironmentBuilder(_implementationStore)
          .Inject(selections)
          .Start();

    /// <inheritdoc/>
    public IEnvironmentBuilder Inject(Selections selections, string? overrideMain = null) => new EnvironmentBuilder(_implementationStore).Inject(selections, overrideMain);
}
