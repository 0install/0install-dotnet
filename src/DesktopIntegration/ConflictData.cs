// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Stores information about an <see cref="AccessPoint"/> causing a conflict and the <see cref="ZeroInstall.DesktopIntegration.AppEntry"/> containing it.
    /// </summary>
    /// <param name="AccessPoint">The <see cref="AccessPoints.AccessPoint"/> causing the conflict.</param>
    /// <param name="AppEntry">The application containing the <paramref name="AccessPoint"/>.</param>
    public sealed record ConflictData(AccessPoint AccessPoint, AppEntry AppEntry);
}
