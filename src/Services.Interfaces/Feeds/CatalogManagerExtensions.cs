// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using NanoByte.Common;
using ZeroInstall.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Provides extension methods for <see cref="ICatalogManager"/>.
    /// </summary>
    public static class CatalogManagerExtensions
    {
        /// <summary>
        /// Loads the last result of <see cref="ICatalogManager.GetOnline"/>.
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
                return manager.GetCached() ?? new Catalog();
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Warn(ex);
                return new();
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn(ex);
                return new();
            }
            catch (InvalidDataException ex)
            {
                Log.Warn(ex);
                return new();
            }
            #endregion
        }

        /// <summary>
        /// Downloads and merges all <see cref="Catalog"/>s specified by the configuration files.
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
            catch (IOException ex)
            {
                Log.Warn(ex);
                return new();
            }
            catch (WebException ex)
            {
                Log.Warn(ex);
                return new();
            }
            catch (InvalidDataException ex)
            {
                Log.Warn(ex);
                return new();
            }
            catch (SignatureException ex)
            {
                Log.Warn(ex);
                return new();
            }
            catch (UriFormatException ex)
            {
                Log.Warn(ex);
                return new();
            }
            #endregion
        }
    }
}
