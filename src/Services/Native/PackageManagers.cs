// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Services.Native;

/// <summary>
/// Provides <see cref="IPackageManager"/> instances.
/// </summary>
public static class PackageManagers
{
    /// <summary>
    /// Creates the default <see cref="IPackageManager"/> for the current platform.
    /// </summary>
    /// <param name="handler">A callback object used when the user needs to be informed about progress.</param>
    public static IPackageManager Default(ITaskHandler? handler = null)
    {
        var packageManagers = new List<IPackageManager>();

        if (WindowsUtils.IsWindows)
        {
            packageManagers.Add(new WindowsPackageManager());
            if (handler != null)
                packageManagers.Add(new WinGetPackageManager(handler));
        }
        //if (UnixUtils.IsUnix) packageManagers.Add(new PackageKitPackageManager());

        return new CompositePackageManager(packageManagers);
    }
}
