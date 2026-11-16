// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Icons;

/// <summary>
/// Provides extension methods for <see cref="IIconStore"/>.
/// </summary>
public static class IconStoreExtensions
{
    extension(IIconStore iconStore)
    {
        /// <summary>
        /// Tries to get an icon that is already cached.
        /// </summary>
        /// <param name="icon">The icon to get.</param>
        /// <returns>The file path of the icon in the cache; <c>null</c> if the icon is not cached yet.</returns>
        public string? TryGetCached(Icon icon)
            => iconStore.TryGetCached(icon, out _);
    }
}
