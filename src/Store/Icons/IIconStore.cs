// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Icons;

/// <summary>
/// Stores icon files downloaded from the web as local files.
/// </summary>
/// <remarks>Implementations of this interface are immutable and thread-safe.</remarks>
public interface IIconStore
{
    /// <summary>
    /// Tries to get an icon that is already cached.
    /// </summary>
    /// <param name="icon">The icon to get.</param>
    /// <param name="stale">Indicates whether the cached file is outdated.</param>
    /// <returns>The file path of the icon in the cache; <c>null</c> if the icon is not cached yet.</returns>
    string? GetCached(Icon icon, out bool stale);

    /// <summary>
    /// Gets an icon from the cache or downloads it if not yet cached.
    /// </summary>
    /// <param name="icon">The icon to get.</param>
    /// <param name="stale">Indicates whether the cached file is outdated.</param>
    /// <returns>The file path of the icon in the cache.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
    /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
    /// <exception cref="InvalidDataException">The icon does not have a valid format.</exception>
    string Get(Icon icon, out bool stale);

    /// <summary>
    /// Gets an icon from the cache or downloads it if not yet cached or outdated.
    /// </summary>
    /// <param name="icon">The icon to get.</param>
    /// <returns>The file path of the icon in the cache.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
    /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
    /// <exception cref="InvalidDataException">The icon does not have a valid format.</exception>
    string GetFresh(Icon icon);
}
