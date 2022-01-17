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
    public static IPackageManager Default()
    {
        if (WindowsUtils.IsWindows) return new WindowsPackageManager();
        //else if (UnixUtils.IsUnix) return new PackageKitPackageManager();
        else return new StubPackageManager();
    }
}
