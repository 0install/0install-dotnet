// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Client;

/// <summary>
/// Runs Zero Install as a child process.
/// </summary>
internal class ZeroInstallLauncher : ProcessLauncher
{
    public ZeroInstallLauncher(string commandLine)
        : base(ProcessUtils.FromCommandLine(commandLine))
    {}

    protected override void HandleExitCode(int exitCode)
    {
        switch (exitCode)
        {
            case 1: // No changes
                break;
            case 10: // Web error
                throw new WebException();
            case 11: // Access denied
                throw new UnauthorizedAccessException();
            case 12: // IO error
                throw new IOException();
            case 100: // User canceled
                throw new OperationCanceledException();
            default:
                base.HandleExitCode(exitCode);
                return;
        }
    }
}
