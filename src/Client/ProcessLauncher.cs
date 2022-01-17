// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common;

namespace ZeroInstall.Client;

/// <summary>
/// Launches an external process.
/// </summary>
[PrimaryConstructor]
internal partial class ProcessLauncher : IProcessLauncher
{
    private readonly string _commandLine;

    /// <inheritdoc />
    public Process Start(params string[] args)
        => ProcessUtils.FromCommandLine(_commandLine + " " + args.JoinEscapeArguments()).Start();

    /// <inheritdoc />
    public int Run(params string[] args)
        => ProcessUtils.FromCommandLine(_commandLine + " " + args.JoinEscapeArguments()).Run();
}
