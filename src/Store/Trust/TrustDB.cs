// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// A database of OpenPGP signature fingerprints the users trusts to sign <see cref="Feed"/>s coming from specific domains.
    /// </summary>
    [XmlRoot("trusted-keys", Namespace = XmlNamespace), XmlType("trusted-keys", Namespace = XmlNamespace)]
    [XmlNamespace("xsi", XmlStorage.XsiNamespace)]
    public sealed class TrustDB : ICloneable<TrustDB>, IEquatable<TrustDB>
    {
        #region Constants
        /// <summary>
        /// The XML namespace used for storing trust-related data.
        /// </summary>
        public const string XmlNamespace = "http://zero-install.sourceforge.net/2007/injector/trust";

        /// <summary>
        /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
        /// </summary>
        public const string XsdLocation = "https://docs.0install.net/specifications/trust.xsd";

        /// <summary>
        /// Provides XML Editors with location hints for XSD files.
        /// </summary>
        [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
        public string XsiSchemaLocation = XmlNamespace + " " + XsdLocation;
        #endregion

        // Order is preserved, but ignore it when comparing

        /// <summary>
        /// A list of known <see cref="Key"/>s.
        /// </summary>
        [XmlElement("key")]
        public List<Key> Keys { get; } = new List<Key>();

        /// <summary>
        /// Determines whether a key is trusted for a specific domain.
        /// </summary>
        /// <param name="fingerprint">The fingerprint of the key to check.</param>
        /// <param name="domain">The domain the key should be valid for.</param>
        public bool IsTrusted(string fingerprint, Domain domain)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
            #endregion

            return Keys.Any(key => key.Fingerprint == fingerprint && key.Domains.Contains(domain));
        }

        /// <summary>
        /// Marks a key as trusted for a specific domain.
        /// </summary>
        /// <param name="fingerprint">The fingerprint of the key to check.</param>
        /// <param name="domain">The domain the key should be valid for.</param>
        public void TrustKey(string fingerprint, Domain domain)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
            #endregion

            Log.Debug($"Trusting {fingerprint} for {domain}");

            var targetKey = Keys.FirstOrDefault(key => key.Fingerprint == fingerprint);
            if (targetKey == null)
            {
                targetKey = new Key {Fingerprint = fingerprint};
                Keys.Add(targetKey);
            }

            targetKey.Domains.Add(domain);
        }

        /// <summary>
        /// Marks a key as no longer trusted for a specific domain.
        /// </summary>
        /// <param name="fingerprint">The fingerprint of the key to check.</param>
        /// <param name="domain">The domain the key should be valid for.</param>
        public void UntrustKey(string fingerprint, Domain domain)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
            #endregion

            Log.Debug($"Untrusting {fingerprint} for {domain}");

            foreach (var key in Keys.Where(key => key.Fingerprint == fingerprint))
                key.Domains.Remove(domain);
        }

        #region Storage
        private string? _filePath;

        /// <summary>
        /// Loads the <see cref="TrustDB"/> from a file.
        /// </summary>
        /// <param name="path">The file to load from.</param>
        /// <returns>The loaded <see cref="TrustDB"/>.</returns>
        /// <exception cref="IOException">A problem occured while reading the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        /// <exception cref="InvalidDataException">A problem occured while deserializing the XML data.</exception>
        public static TrustDB Load(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            Log.Debug("Loading trust database from: " + path);
            var trustDB = XmlStorage.LoadXml<TrustDB>(path);
            trustDB._filePath = path;
            return trustDB;
        }

        /// <summary>
        /// The default file path used to store the <see cref="TrustDB"/>.
        /// </summary>
        public static string DefaultLocation => Locations.GetSaveConfigPath("0install.net", true, "injector", "trustdb.xml");

        /// <summary>
        /// Tries to load the <see cref="TrustDB"/> from the <see cref="DefaultLocation"/>. Automatically falls back to defaults on errors.
        /// </summary>
        /// <returns>The loaded <see cref="TrustDB"/> or an empty <see cref="TrustDB"/> if there was a problem.</returns>
        public static TrustDB LoadSafe()
        {
            try
            {
                return Load(DefaultLocation);
            }
            catch (FileNotFoundException)
            {
                return new TrustDB {_filePath = DefaultLocation}; // Start empty and save new file
            }
            catch (IOException ex)
            {
                Log.Warn(Resources.ErrorLoadingTrustDB);
                Log.Warn(ex);
                return new TrustDB(); // Start empty but do not overwrite existing file
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn(Resources.ErrorLoadingTrustDB);
                Log.Warn(ex);
                return new TrustDB(); // Start empty but do not overwrite existing file
            }
            catch (InvalidDataException ex)
            {
                Log.Warn(Resources.ErrorLoadingTrustDB);
                Log.Warn(ex);
                return new TrustDB(); // Start empty but do not overwrite existing file
            }
        }

        /// <summary>
        /// Saves the this <see cref="TrustDB"/> to the location it was loaded from if possible.
        /// </summary>
        /// <returns><c>true</c> if the file was saved; <c>false</c> if</returns>
        /// <exception cref="IOException">A problem occured while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
        public bool Save()
        {
            if (_filePath == null)
            {
                Log.Warn("Trust database was not loaded from disk and can therefore not be saved");
                return false;
            }
            else
            {
                Save(_filePath);
                return true;
            }
        }

        /// <summary>
        /// Saves this <see cref="TrustDB"/> to a file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <exception cref="IOException">A problem occured while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
        public void Save(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            Log.Debug("Saving trust database to: " + path);
            this.SaveXml(path);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="TrustDB"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="TrustDB"/>.</returns>
        public TrustDB Clone()
        {
            var trust = new TrustDB();
            trust.Keys.AddRange(Keys.CloneElements());
            return trust;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(TrustDB? other)
            => other != null && Keys.UnsequencedEquals(other.Keys);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is TrustDB other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => Keys.GetUnsequencedHashCode();
        #endregion
    }
}
