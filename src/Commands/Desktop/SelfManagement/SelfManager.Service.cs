// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System.Diagnostics;
using NanoByte.Common.Native;
using System.ServiceProcess;
#endif

namespace ZeroInstall.Commands.Desktop.SelfManagement;

partial class SelfManager
{
#if NETFRAMEWORK
    private const string ServiceName = "0store-service";
    private static readonly string _installUtilExe = Path.Combine(WindowsUtils.GetNetFxDirectory(WindowsUtils.NetFx40), "InstallUtil.exe");

    private string ServiceExe => Path.Combine(TargetDir, ServiceName + ".exe");

    private static ServiceController? GetServiceController()
        => ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == ServiceName);
#endif

    /// <summary>
    /// Stops the Zero Install Store Service if it is running.
    /// </summary>
    private void ServiceStop()
    {
#if NETFRAMEWORK
        if (!WindowsUtils.IsWindows) return;

        // Determine whether the service is installed and running
        var service = GetServiceController();
        if (service?.Status != ServiceControllerStatus.Running) return;

        // Determine whether the service is installed in the target directory we are updating
        string? imagePath = RegistryUtils.GetString(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\" + ServiceName, "ImagePath")?.Trim('"');
        if (imagePath == null || !imagePath.StartsWith(TargetDir)) return;

        Handler.RunTask(new SimpleTask(Resources.StopService, () =>
        {
            try
            {
                service.Stop();
            }
            #region Error handling
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException("Failed to stop service.", ex);
            }
            #endregion

            Thread.Sleep(2000);
        }));
#endif
    }

    /// <summary>
    /// Starts the Zero Install Store Service.
    /// </summary>
    /// <remarks>Must be called after <see cref="TargetMutexRelease"/>.</remarks>
    private void ServiceStart()
    {
#if NETFRAMEWORK
        Handler.RunTask(new SimpleTask(Resources.StartService, () =>
        {
            try
            {
                GetServiceController()?.Start();
            }
            #region Error handling
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException("Failed to start service.", ex);
            }
            #endregion
        }));
#endif
    }

    /// <summary>
    /// Installs the Zero Install Store Service.
    /// </summary>
    private bool ServiceInstall()
    {
#if NETFRAMEWORK
        if (!WindowsUtils.IsWindows) return false;
        if (!File.Exists(ServiceExe))
        {
            Log.Warn(string.Format(Resources.FileOrDirNotFound, ServiceExe));
            return false;
        }

        Handler.RunTask(new SimpleTask(Resources.InstallService, () =>
            new ProcessStartInfo(_installUtilExe, ServiceExe.EscapeArgument())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            }.Run()));
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// Uninstalls the Zero Install Store Service.
    /// </summary>
    private void ServiceUninstall()
    {
#if NETFRAMEWORK
        if (!WindowsUtils.IsWindows) return;

        Handler.RunTask(new SimpleTask(Resources.UninstallService, () =>
            new ProcessStartInfo(_installUtilExe, new[] {"/u", ServiceExe}.JoinEscapeArguments())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            }.Run()));
#endif
    }

    /// <summary>
    /// Deletes log files left by the installation of the service.
    /// </summary>
    private void DeleteServiceLogFiles()
    {
        File.Delete(Path.Combine(TargetDir, "0store-service.InstallLog"));
        File.Delete(Path.Combine(TargetDir, "InstallUtil.InstallLog"));
    }
}
