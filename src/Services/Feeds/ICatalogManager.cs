// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Provides access to remote and local <see cref="Catalog"/>s. Handles downloading, signature verification and caching.
/// </summary>
public interface ICatalogManager
{
    /// <summary>
    /// Downloads and merges all <see cref="Catalog"/>s specified by the configuration files.
    /// </summary>
    /// <returns>The merged <see cref="Catalog"/>s.</returns>
    /// <exception cref="IOException">A problem occurred while reading a local catalog file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a local catalog file was not permitted.</exception>
    /// <exception cref="WebException">A problem occurred while fetching a remote catalog file.</exception>
    /// <exception cref="NotSupportedException">The catalog requires a newer version of Zero Install.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    /// <exception cref="SignatureException">The signature data of a remote catalog file could not be verified.</exception>
    /// <exception cref="UriFormatException">An invalid catalog source is specified in the configuration file.</exception>
    Catalog GetOnline();

    /// <summary>
    /// Tries to return a locally cached copy of the merged <see cref="Catalog"/>s specified by the configuration files, as previously returned by <see cref="GetOnline"/>.
    /// </summary>
    /// <returns>The merged <see cref="Catalog"/>s; <c>null</c> if there was a problem loading them.</returns>
    Catalog? TryGetCached();

    /// <summary>
    /// Downloads and normalizes a remote catalog file.
    /// </summary>
    /// <param name="source">The URL to download the catalog file from.</param>
    /// <returns>The parsed <see cref="Catalog"/>.</returns>
    /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
    /// <exception cref="SignatureException">The signature data of a remote catalog file could not be verified.</exception>
    /// <exception cref="NotSupportedException">The catalog requires a newer version of Zero Install.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    Catalog DownloadCatalog(FeedUri source);

    /// <summary>
    /// Returns a list of catalog sources as defined by configuration files.
    /// </summary>
    /// <exception cref="IOException">There was a problem accessing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    /// <exception cref="UriFormatException">An invalid catalog source is specified in the configuration file.</exception>
    public FeedUri[] GetSources();

    /// <summary>
    /// Adds a new source to download <see cref="Catalog"/> files from.
    /// </summary>
    /// <param name="uri">The URI of the source to add.</param>
    /// <returns><c>true</c> if the source was add; <c>false</c> if the source was already in the list.</returns>
    /// <exception cref="IOException">There was a problem accessing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    /// <exception cref="UriFormatException">An invalid catalog source is specified in the configuration file.</exception>
    bool AddSource(FeedUri uri);

    /// <summary>
    /// Removes an existing source of <see cref="Catalog"/> files.
    /// </summary>
    /// <param name="uri">The URI of the source to remove.</param>
    /// <returns><c>true</c> if the source was removed; <c>false</c> if the source was not in the current list.</returns>
    /// <exception cref="IOException">There was a problem accessing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    /// <exception cref="UriFormatException">An invalid catalog source is specified in the configuration file.</exception>
    bool RemoveSource(FeedUri uri);
}
