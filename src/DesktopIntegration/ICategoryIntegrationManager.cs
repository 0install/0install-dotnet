// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Manages desktop integration via <see cref="AccessPoint"/>s, grouping them into categories.
/// </summary>
public interface ICategoryIntegrationManager : IIntegrationManager
{
    /// <summary>
    /// Applies a category of <see cref="AccessPoint"/>s for an application.
    /// </summary>
    /// <param name="appEntry">The application being integrated.</param>
    /// <param name="feed">The feed providing additional metadata, icons, etc. for the application.</param>
    /// <param name="categories">A list of all <see cref="AccessPoint"/> categories to be added to the already applied ones.</param>
    /// <exception cref="ConflictException">One or more of the <paramref name="categories"/> would cause a conflict with the existing <see cref="AccessPoint"/>s in <see cref="AppList"/>.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    void AddAccessPointCategories(AppEntry appEntry, Feed feed, params IReadOnlyList<string> categories);

    /// <summary>
    /// Removes a category of already applied <see cref="AccessPoint"/>s for an application.
    /// </summary>
    /// <param name="appEntry">The application being integrated.</param>
    /// <param name="categories">A list of all <see cref="AccessPoint"/> categories to be removed from the already applied ones.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    void RemoveAccessPointCategories(AppEntry appEntry, params IReadOnlyList<string> categories);
}
