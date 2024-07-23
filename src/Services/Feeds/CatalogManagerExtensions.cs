// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Provides extension methods for <see cref="ICatalogManager"/>.
/// </summary>
public static class CatalogManagerExtensions
{
    /// <summary>
    /// Tries to return the result of the last successful <see cref="ICatalogManager.GetOnline"/> call.
    /// </summary>
    /// <returns>A <see cref="Catalog"/>; an empty <see cref="Catalog"/> if there was a problem.</returns>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "File system access")]
    public static Catalog GetCachedSafe(this ICatalogManager manager)
    {
        #region Sanity checks
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        #endregion

        try
        {
            return manager.GetCached() ?? new();
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Log.Warn(Resources.ErrorLoadingCatalog, ex);
            return new();
        }
        #endregion
    }

    /// <summary>
    /// Tries to download and merge all <see cref="Catalog"/>s specified by the configuration files.
    /// </summary>
    /// <returns>A <see cref="Catalog"/>; an empty <see cref="Catalog"/> if there was a problem.</returns>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Performs network IO and has side-effects")]
    public static Catalog GetOnlineSafe(this ICatalogManager manager)
    {
        #region Sanity checks
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        #endregion

        try
        {
            return manager.GetOnline();
        }
        #region Error handling
        catch (Exception ex) when (ex is UriFormatException or WebException or IOException or UnauthorizedAccessException or InvalidDataException or SignatureException)
        {
            Log.Warn(Resources.ErrorLoadingCatalog, ex);
            return new();
        }
        #endregion
    }
}
