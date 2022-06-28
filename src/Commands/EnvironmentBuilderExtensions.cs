// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Commands.Basic;
using ZeroInstall.Services.Executors;

namespace ZeroInstall.Commands;

/// <summary>
/// Provides extension methods for <see cref="IEnvironmentBuilder"/>.
/// </summary>
public static class EnvironmentBuilderExtensions
{
    /// <summary>
    /// Adds environment variables that allow the program to make calls back to Zero Install.
    /// </summary>
    public static IEnvironmentBuilder SetCallbackEnvironmentVariables(this IEnvironmentBuilder builder)
    {
        void TryAdd(string envName, ProcessStartInfo? startInfo)
        {
            if (startInfo == null) return;

            try
            {
                builder.SetEnvironmentVariable(envName, startInfo.ToCommandLine());
            }
            catch (FileNotFoundException)
            {
                // Zero Install may be embedded as a library rather than called as an executable
            }
        }

        TryAdd(ZeroInstallEnvironment.CliName, ProgramUtils.CliStartInfo());
        TryAdd(ZeroInstallEnvironment.GuiName, ProgramUtils.GuiStartInfo());
        TryAdd(ZeroInstallEnvironment.ExternalFetcherName, ProgramUtils.CliStartInfo(Fetch.Name));

        return builder;
    }
}
