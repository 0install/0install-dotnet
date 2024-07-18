// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Fluent-style builder for a process execution environment.
/// </summary>
public interface IEnvironmentBuilder
{
    /// <summary>
    /// Instead of executing the selected program directly, pass it as an argument to this program. Useful for debuggers. May contain command-line arguments. Whitespaces must be escaped!
    /// </summary>
    /// <returns>The execution environment. Reference to self for fluent API use.</returns>
    IEnvironmentBuilder AddWrapper(string? wrapper);

    /// <summary>
    /// Appends user specified <paramref name="arguments"/> to the command-line.
    /// </summary>
    /// <returns>The execution environment. Reference to self for fluent API use.</returns>
    IEnvironmentBuilder AddArguments(params string[] arguments);

    /// <summary>
    /// Sets an environment variable in the execution environment.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value to set the environment variable to.</param>
    /// <returns>The execution environment. Reference to self for fluent API use.</returns>
    IEnvironmentBuilder SetEnvironmentVariable(string name, string value);

    /// <summary>
    /// Builds a <see cref="ProcessStartInfo"/> for starting the program.
    /// </summary>
    /// <returns>The <see cref="ProcessStartInfo"/> that can be used to start the new <see cref="Process"/>.</returns>
    /// <exception cref="ImplementationNotFoundException">One of the <see cref="Implementation"/>s is not cached yet.</exception>
    /// <exception cref="ExecutorException">The <see cref="IExecutor"/> was unable to process the <see cref="Selections"/>.</exception>
    /// <exception cref="IOException">A problem occurred while writing a file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a file is not permitted.</exception>
    ProcessStartInfo ToStartInfo();

    /// <summary>
    /// Starts the program.
    /// </summary>
    /// <returns>The newly created <see cref="Process"/>.</returns>
    /// <exception cref="ImplementationNotFoundException">One of the <see cref="Implementation"/>s is not cached yet.</exception>
    /// <exception cref="ExecutorException">The <see cref="IExecutor"/> was unable to process the <see cref="Selections"/> or the main executable could not be launched.</exception>
    /// <exception cref="FileNotFoundException">Failed to find the main executable.</exception>
    /// <exception cref="IOException">Failed to start the program.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a file is not permitted.</exception>
    Process? Start();
}
