// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Execution context wrapping a standard <see cref="ProcessStartInfo"/> for native process execution.
/// </summary>
public class NativeExecutionContext : IExecutionContext
{
    /// <summary>
    /// The underlying ProcessStartInfo being built.
    /// </summary>
    public ProcessStartInfo StartInfo { get; } = new() { UseShellExecute = false };

#if NET
    private IDictionary<string, string?> EnvironmentVariables => StartInfo.Environment;
#else
    private System.Collections.Specialized.StringDictionary EnvironmentVariables => StartInfo.EnvironmentVariables;
#endif

    /// <inheritdoc/>
    public void SetEnvironmentVariable(string name, string value)
        => EnvironmentVariables[name] = value;

    /// <inheritdoc/>
    public string? GetEnvironmentVariable(string name)
        => EnvironmentVariables.ContainsKey(name) ? EnvironmentVariables[name] : null;

    /// <inheritdoc/>
    public bool ContainsEnvironmentVariable(string name)
        => EnvironmentVariables.ContainsKey(name);

    /// <inheritdoc/>
    public void SetWorkingDirectory(string path)
        => StartInfo.WorkingDirectory = path;

    /// <inheritdoc/>
    public ProcessStartInfo ToStartInfo()
        => StartInfo;
}
