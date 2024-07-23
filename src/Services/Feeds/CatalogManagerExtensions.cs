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
    /// Downloads and merges all <see cref="Catalog"/>s specified by the configuration files or returns a cached copy, if available.
    /// </summary>
    /// <returns>The merged <see cref="Catalog"/>s.</returns>
    /// <exception cref="IOException">A problem occurred while reading a local catalog file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a local catalog file was not permitted.</exception>
    /// <exception cref="WebException">A problem occurred while fetching a remote catalog file.</exception>
    /// <exception cref="NotSupportedException">The catalog requires a newer version of Zero Install.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    /// <exception cref="SignatureException">The signature data of a remote catalog file could not be verified.</exception>
    /// <exception cref="UriFormatException">An invalid catalog source is specified in the configuration file.</exception>
    public static Catalog Get(this ICatalogManager manager)
        => manager.TryGetCached()
        ?? manager.GetOnline();

    /// <summary>
    /// Tries to download and merge all <see cref="Catalog"/>s specified by the configuration files.
    /// </summary>
    /// <returns>The merged <see cref="Catalog"/>s; <c>null</c> if there was a problem loading them.</returns>
    public static Catalog? TryGetOnline(this ICatalogManager manager)
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
            return null;
        }
        #endregion
    }
}
