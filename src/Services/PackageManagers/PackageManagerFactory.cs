// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Services.PackageManagers
{
    /// <summary>
    /// Creates <see cref="IPackageManager"/> instances.
    /// </summary>
    public static class PackageManagerFactory
    {
        /// <summary>
        /// Creates an <see cref="IPackageManager"/> instance.
        /// </summary>
        public static IPackageManager Create()
        {
            if (WindowsUtils.IsWindows) return new WindowsPackageManager();
            //else if (UnixUtils.IsUnix) return new PackageKitPackageManager();
            else return new StubPackageManager();
        }
    }
}
