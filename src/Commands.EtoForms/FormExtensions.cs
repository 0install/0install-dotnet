// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Commands.EtoForms;

/// <summary>
/// Provides extension methods for <see cref="Window"/>s.
/// </summary>
internal static class FormExtensions
{
    /// <summary>
    /// Prevents the window from being pinned to the taskbar if the current Zero Install instance is not integrated into the desktop environment.
    /// </summary>
    /// <remarks>Has no effect on platforms other than Windows.</remarks>
    public static void PreventPinningIfNotIntegrated(this Window window)
    {
        if (!ZeroInstallInstance.IsIntegrated) window.PreventPinning();
    }
}
