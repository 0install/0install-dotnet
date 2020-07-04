// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using ZeroInstall.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Provides access to remote and local <see cref="Catalog"/>s. Handles downloading, signature verification and caching.
    /// </summary>
    public interface ICatalogManager
    {
        /// <summary>
        /// Loads the last result of <see cref="GetOnline"/>.
        /// </summary>
        /// <returns>A <see cref="Catalog"/>; <c>null</c> if there is no cached data.</returns>
        /// <exception cref="IOException">A problem occurred while reading the cache file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the cache file was not permitted.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "File system access")]
        Catalog? GetCached();

        /// <summary>
        /// Downloads and merges all <see cref="Catalog"/>s specified by the configuration files.
        /// </summary>
        /// <returns>A <see cref="Catalog"/>.</returns>
        /// <exception cref="IOException">A problem occurred while reading a local catalog file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to a local catalog file was not permitted.</exception>
        /// <exception cref="WebException">A problem occurred while fetching a remote catalog file.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
        /// <exception cref="SignatureException">The signature data of a remote catalog file could not be verified.</exception>
        /// <exception cref="UriFormatException">An invalid catalog source is specified in the configuration file.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Performs network IO and has side-effects")]
        Catalog GetOnline();

        /// <summary>
        /// Downloads and parses a remote catalog file. Mainly for internal use.
        /// </summary>
        /// <param name="source">The URL to download the catalog file from.</param>
        /// <returns>The parsed <see cref="Catalog"/>.</returns>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="SignatureException">The signature data of a remote catalog file could not be verified.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
        Catalog DownloadCatalog(FeedUri source);

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
}
