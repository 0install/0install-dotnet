// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains control logic for applying <see cref="AccessPoints.AppAlias"/> on Windows systems.
/// </summary>
[SupportedOSPlatform("windows")]
public static class AppAlias
{
    #region Constants
    /// <summary>The HKCU/HKLM registry key for storing application lookup paths.</summary>
    public const string RegKeyAppPaths = @"Software\Microsoft\Windows\CurrentVersion\App Paths";
    #endregion

    #region Create
    /// <summary>
    /// Creates an application alias in the current system.
    /// </summary>
    /// <param name="target">The application being integrated.</param>
    /// <param name="command">The command within <paramref name="target"/> the alias shall point to; can be <c>null</c>.</param>
    /// <param name="aliasName">The name of the alias to be created.</param>
    /// <param name="machineWide">Create the alias machine-wide instead of just for the current user.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public static void Create(FeedTarget target, string? command, string aliasName, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(aliasName)) throw new ArgumentNullException(nameof(aliasName));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

#if NETFRAMEWORK
        string stubDirPath = GetStubDir(machineWide);
        PathEnv.AddDir(stubDirPath, machineWide);

        string stubFilePath = Path.Combine(stubDirPath, $"{aliasName}.exe");
        new StubBuilder(iconStore).BuildRunStub(stubFilePath, target, command);

        if (machineWide || WindowsUtils.IsWindows7)
        {
            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;
            using var appPathsKey = hive.CreateSubKeyChecked(RegKeyAppPaths);
            using var exeKey = appPathsKey.CreateSubKeyChecked($"{aliasName}.exe");
            exeKey.SetValue("", stubFilePath);
        }
#else
            throw new PlatformNotSupportedException("Generating Windows aliases is not supported by the .NET Core version of Zero Install.");
#endif
    }
    #endregion

    #region Remove
    /// <summary>
    /// Removes an application alias from the current system.
    /// </summary>
    /// <param name="aliasName">The name of the alias to be removed.</param>
    /// <param name="machineWide">The alias was created machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public static void Remove(string aliasName, bool machineWide)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(aliasName)) throw new ArgumentNullException(nameof(aliasName));
        #endregion

        RemoveFromAppPaths($"{aliasName}.exe", machineWide);

        string stubFilePath = Path.Combine(GetStubDir(machineWide), $"{aliasName}.exe");
        if (File.Exists(stubFilePath)) File.Delete(stubFilePath);
    }

    /// <summary>
    /// Removes an EXE from the AppPath registry key.
    /// </summary>
    /// <param name="exeName">The name of the EXE file to add (including the file ending).</param>
    /// <param name="machineWide"><c>true</c> to use the machine-wide registry key; <c>false</c> for the per-user variant.</param>
    private static void RemoveFromAppPaths(string exeName, bool machineWide)
    {
        var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;
        hive.TryDeleteSubKey($@"{RegKeyAppPaths}\{exeName}");
    }
    #endregion

    /// <summary>
    /// Returns the path of the directory used to store alias stub EXEs.
    /// </summary>
    /// <param name="machineWide"><c>true</c> for a machine-wide directory; <c>false</c> for a directory just for the current user.</param>
    public static string GetStubDir(bool machineWide)
        => IntegrationManager.GetDir(machineWide, "aliases");
}
