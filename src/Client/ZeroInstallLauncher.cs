// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
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

    protected override void WaitForExit(Process process)
    {
        try
        {
            base.WaitForExit(process);
        }
        catch (ExitCodeException ex)
        {
            switch (ex.ExitCode)
            {
                case 1: // No changes
                    break;
                case 10: // Web error
                    throw new WebException(ex.Message, ex);
                case 11: // Access denied
                    throw new UnauthorizedAccessException(ex.Message, ex);
                case 12: // IO error
                    throw new IOException(ex.Message, ex);
                case 100: // User canceled
                    throw new OperationCanceledException();
                default:
                    throw;
            }
        }
    }
}
