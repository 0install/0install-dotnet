// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Manages the PATH environment variable.
/// </summary>
public static class PathEnv
{
    /// <summary>
    /// Adds a directory to the search PATH.
    /// </summary>
    /// <param name="directory">The directory to add to the search PATH.</param>
    /// <param name="machineWide"><c>true</c> to use the machine-wide PATH variable; <c>false</c> for the per-user variant.</param>
    public static void AddDir(string directory, bool machineWide)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException(nameof(directory));
        #endregion

        var currentPath = Get(machineWide);
        if (!currentPath.Contains(directory)) Set(currentPath.Append(directory), machineWide);
    }

    /// <summary>
    /// Removes a directory from the search PATH.
    /// </summary>
    /// <param name="directory">The directory to remove from the search PATH.</param>
    /// <param name="machineWide"><c>true</c> to use the machine-wide PATH variable; <c>false</c> for the per-user variant.</param>
    public static void RemoveDir(string directory, bool machineWide)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException(nameof(directory));
        #endregion

        var currentPath = Get(machineWide);
        Set(currentPath.Except(directory).ToArray(), machineWide);
    }

    /// <summary>
    /// Returns the current search PATH.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to use the machine-wide PATH variable; <c>false</c> for the per-user variant.</param>
    /// <returns>The individual directories listed in the search path.</returns>
    public static string[] Get(bool machineWide)
    {
        string? value = Environment.GetEnvironmentVariable(
            variable: "Path",
            target: machineWide ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User);
        return string.IsNullOrEmpty(value) ? Array.Empty<string>() : value.Split(Path.PathSeparator);
    }

    /// <summary>
    /// Sets the current search PATH.
    /// </summary>
    /// <param name="directories">The individual directories to list in the search PATH.</param>
    /// <param name="machineWide"><c>true</c> to use the machine-wide PATH variable; <c>false</c> for the per-user variant.</param>
    public static void Set(string[] directories, bool machineWide)
    {
        #region Sanity checks
        if (directories == null) throw new ArgumentNullException(nameof(directories));
        #endregion

        Environment.SetEnvironmentVariable(
            variable: "Path",
            value: StringUtils.Join(Path.PathSeparator.ToString(), directories),
            target: machineWide ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User);
        if (WindowsUtils.IsWindows) WindowsUtils.NotifyEnvironmentChanged();
    }
}
