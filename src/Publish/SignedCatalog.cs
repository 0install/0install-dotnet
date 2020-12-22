// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// A wrapper around a <see cref="Catalog"/> adding and XSL stylesheet and a digital signature.
    /// </summary>
    [Serializable]
    public class SignedCatalog
    {
        /// <summary>
        /// The wrapped <see cref="Catalog"/>.
        /// </summary>
        public Catalog Catalog { get; }

        /// <summary>
        /// The secret key used to sign the <see cref="Catalog"/>; <c>null</c> for no signature.
        /// </summary>
        public OpenPgpSecretKey? SecretKey { get; set; }

        private readonly IOpenPgp _openPgp;

        /// <summary>
        /// Creates a new signed catalog.
        /// </summary>
        /// <param name="catalog">The wrapped <see cref="Catalog"/>.</param>
        /// <param name="secretKey">The secret key used to sign the <see cref="Catalog"/>; <c>null</c> for no signature.</param>
        /// <param name="openPgp">The OpenPGP-compatible system used to create the signatures; <c>null</c> for default.</param>
        public SignedCatalog(Catalog catalog, OpenPgpSecretKey? secretKey, IOpenPgp? openPgp = null)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            SecretKey = secretKey;
            _openPgp = openPgp ?? OpenPgp.Signing();
        }

        /// <summary>
        /// Loads a <see cref="Catalog"/> from an XML file and identifies the signature (if any).
        /// </summary>
        /// <param name="path">The file to load from.</param>
        /// <returns>The loaded <see cref="SignedCatalog"/>.</returns>
        /// <exception cref="IOException">A problem occurred while reading the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
        public static SignedCatalog Load(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            var openPgp = OpenPgp.Signing();
            return new(XmlStorage.LoadXml<Catalog>(path), FeedUtils.GetKey(path, openPgp), openPgp);
        }

        /// <summary>
        /// Saves <see cref="Catalog"/> to an XML file, adds the default stylesheet and sign it it with <see cref="SecretKey"/> (if specified).
        /// </summary>
        /// <remarks>Writing and signing the catalog file are performed as an atomic operation (i.e. if signing fails an existing file remains unchanged).</remarks>
        /// <param name="path">The file to save in.</param>
        /// <param name="passphrase">The passphrase to use to unlock the secret key; can be <c>null</c> if <see cref="SecretKey"/> is <c>null</c>.</param>
        /// <exception cref="IOException">A problem occurred while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
        /// <exception cref="KeyNotFoundException">The specified <see cref="SecretKey"/> could not be found on the system.</exception>
        /// <exception cref="WrongPassphraseException"><paramref name="passphrase"/> was incorrect.</exception>
        public void Save(string path, string? passphrase = null)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            if (SecretKey == null)
            {
                Catalog.SaveXml(path);
                return;
            }

            using (var stream = new MemoryStream())
            {
                Catalog.SaveXml(stream, stylesheet: @"catalog.xsl");
                stream.Position = 0;

                FeedUtils.SignFeed(stream, SecretKey, passphrase, _openPgp);
                stream.CopyToFile(path);
            }
            string directory = Path.GetDirectoryName(path)!;
            _openPgp.DeployPublicKey(SecretKey, directory);
            FeedUtils.DeployStylesheet(directory, @"catalog");
        }
    }
}
