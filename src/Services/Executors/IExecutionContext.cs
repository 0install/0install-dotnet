// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Represents a context for building a process execution environment.
/// Abstracts the details of how the environment is configured for different execution strategies.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Sets an environment variable in the execution context.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value to set.</param>
    void SetEnvironmentVariable(string name, string value);

    /// <summary>
    /// Gets the value of an environment variable in the execution context.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if not set.</returns>
    string? GetEnvironmentVariable(string name);

    /// <summary>
    /// Checks if an environment variable is set in the execution context.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>True if the variable is set, false otherwise.</returns>
    bool ContainsEnvironmentVariable(string name);

    /// <summary>
    /// Sets the working directory for the execution.
    /// </summary>
    /// <param name="path">The working directory path.</param>
    void SetWorkingDirectory(string path);

    /// <summary>
    /// Converts the execution context to a ProcessStartInfo for starting a process.
    /// </summary>
    /// <returns>A ProcessStartInfo configured for this execution context.</returns>
    ProcessStartInfo ToStartInfo();
}
