// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using NanoByte.Common.Native;
using System.ServiceProcess;
#endif

namespace ZeroInstall.Commands.Desktop;

partial class SelfManager
{
#if NETFRAMEWORK
    private const string ServiceName = "0store-service";
    private static readonly string _installUtilExe = Path.Combine(WindowsUtils.GetNetFxDirectory(WindowsUtils.NetFx40), "InstallUtil.exe");

    private string ServiceExe => Path.Combine(TargetDir, $"{ServiceName}.exe");

    private static ServiceController? GetServiceController()
        => ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == ServiceName);
#endif

    /// <summary>
    /// Stops the Zero Install Store Service if it is running.
    /// </summary>
    private void ServiceStop()
    {
#if NETFRAMEWORK
        if (!WindowsUtils.IsWindowsNT) return;

        // Determine whether the service is installed and running
        var service = GetServiceController();
        if (service?.Status != ServiceControllerStatus.Running) return;

        // Stop existing service if it is installed in the target directory we are updating
        if (RegistryUtils.GetString($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{ServiceName}", "ImagePath")?.Trim('"') is {} imagePath
         && imagePath.StartsWith(TargetDir))
        {
            Handler.RunTask(new ActionTask(Resources.StopService, () =>
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
        }
#endif
    }

    /// <summary>
    /// Starts the Zero Install Store Service.
    /// </summary>
    /// <remarks>Must be called after <see cref="MutexRelease"/>.</remarks>
    private void ServiceStart()
    {
#if NETFRAMEWORK
        Handler.RunTask(new ActionTask(Resources.StartService, () =>
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
        if (!WindowsUtils.IsWindowsNT) return false;
        if (!File.Exists(ServiceExe))
        {
            Log.Warn(string.Format(Resources.FileOrDirNotFound, ServiceExe));
            return false;
        }

        Handler.RunTask(new ActionTask(Resources.InstallService, () => RunHidden(_installUtilExe, ServiceExe)));
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
        if (!WindowsUtils.IsWindowsNT) return;

        Handler.RunTask(new ActionTask(Resources.UninstallService,
            () => RunHidden(_installUtilExe, "/u", ServiceExe)));
#endif
    }

    /// <summary>
    /// Deletes log files left by the installation of the service.
    /// </summary>
    private void DeleteServiceLogFiles()
    {
        try
        {
            File.Delete(Path.Combine(TargetDir, "0store-service.InstallLog"));
            File.Delete(Path.Combine(TargetDir, "InstallUtil.InstallLog"));
        }
        #region Error handling
        catch (IOException ex)
        {
            Log.Info("Failed to remove log files", ex);
        }
        #endregion
    }
}
