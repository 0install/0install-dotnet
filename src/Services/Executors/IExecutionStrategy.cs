// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Defines how to execute applications in a specific environment (native, Docker, Wine, WSL, etc.).
/// </summary>
/// <remarks>Implementations of this interface are immutable and thread-safe.</remarks>
public interface IExecutionStrategy
{
    /// <summary>
    /// Creates a new execution context for building a process execution environment.
    /// </summary>
    IExecutionContext CreateContext();

    /// <summary>
    /// Gets a path mapper for translating paths between the host and target execution environment.
    /// </summary>
    IPathMapper PathMapper { get; }

    /// <summary>
    /// Applies an environment binding in the target execution environment.
    /// </summary>
    /// <param name="context">The execution context being built.</param>
    /// <param name="binding">The binding to apply.</param>
    /// <param name="implementation">The implementation to be made available.</param>
    /// <param name="implementationPath">The local path to the implementation.</param>
    void ApplyEnvironmentBinding(IExecutionContext context, EnvironmentBinding binding, ImplementationSelection implementation, string implementationPath);

    /// <summary>
    /// Deploys an executable for use with ExecutableInVar or ExecutableInPath bindings.
    /// </summary>
    /// <param name="context">The execution context being built.</param>
    /// <param name="name">The name of the executable.</param>
    /// <param name="commandLine">The command-line to execute.</param>
    /// <returns>The path to the deployed executable in the target environment.</returns>
    string DeployExecutable(IExecutionContext context, string name, IEnumerable<string> commandLine);

    /// <summary>
    /// Prepares the execution context for starting the process.
    /// </summary>
    /// <param name="context">The execution context to finalize.</param>
    /// <param name="mainExecutable">The main executable path (may be translated).</param>
    /// <param name="arguments">The command-line arguments.</param>
    void FinalizeExecution(IExecutionContext context, string mainExecutable, string arguments);

    /// <summary>
    /// Starts the process using the prepared execution context.
    /// </summary>
    /// <param name="context">The execution context to use for starting the process.</param>
    /// <returns>The newly created <see cref="Process"/>.</returns>
    Process Start(IExecutionContext context);
}
