// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Model;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Stores a list of applications and the kind of desktop integration the user chose for them.
    /// </summary>
    [XmlRoot("app-list", Namespace = XmlNamespace), XmlType("app-list", Namespace = XmlNamespace)]
    [XmlNamespace("xsi", XmlStorage.XsiNamespace)]
    //[XmlNamespace("caps", CapabilityList.XmlNamespace)]
    //[XmlNamespace("feed", Feed.XmlNamespace)]
    public sealed class AppList : XmlUnknown, ICloneable<AppList>, IEquatable<AppList>
    {
        #region Constants
        /// <summary>
        /// The XML namespace used for storing application list data.
        /// </summary>
        public const string XmlNamespace = "http://0install.de/schema/desktop-integration/app-list";

        /// <summary>
        /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
        /// </summary>
        public const string XsdLocation = XmlNamespace + "/app-list.xsd";

        /// <summary>
        /// Provides XML Editors with location hints for XSD files.
        /// </summary>
        [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
        public string XsiSchemaLocation = XmlNamespace + " " + XsdLocation;
        #endregion

        /// <summary>
        /// A list of <see cref="AppEntry"/>s.
        /// </summary>
        [Description("A list of application entries.")]
        [XmlElement("app")]
        public List<AppEntry> Entries { get; } = new List<AppEntry>();

        /// <summary>
        /// Checks whether an <see cref="AppEntry"/> for a specific interface URI exists.
        /// </summary>
        /// <param name="interfaceUri">The <see cref="AppEntry.InterfaceUri"/> to look for.</param>
        /// <returns><c>true</c> if a matching entry was found; <c>false</c> otherwise.</returns>
        public bool ContainsEntry(FeedUri interfaceUri)
        {
            #region Sanity checks
            if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
            #endregion

            return Entries.Any(entry => entry.InterfaceUri == interfaceUri);
        }

        /// <summary>
        /// Gets an <see cref="AppEntry"/> for a specific interface URI.
        /// </summary>
        /// <param name="interfaceUri">The <see cref="AppEntry.InterfaceUri"/> to look for.</param>
        /// <returns>The first matching <see cref="AppEntry"/>.</returns>
        /// <exception cref="KeyNotFoundException">No entry matching the interface URI was found.</exception>
        public AppEntry this[FeedUri interfaceUri]
        {
            get
            {
                #region Sanity checks
                if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
                #endregion

                try
                {
                    return Entries.First(entry => entry.InterfaceUri == interfaceUri);
                }
                #region Error handling
                catch (InvalidOperationException)
                {
                    throw new KeyNotFoundException(string.Format(Resources.AppNotInList, interfaceUri));
                }
                #endregion
            }
        }

        /// <summary>
        /// Gets an <see cref="AppEntry"/> for a specific interface URI. Safe for missing elements.
        /// </summary>
        /// <param name="interfaceUri">The <see cref="AppEntry.InterfaceUri"/> to look for.</param>
        /// <returns>The first matching <see cref="AppEntry"/>; <c>null</c> if no match was found.</returns>
        public AppEntry? GetEntry(FeedUri interfaceUri)
        {
            #region Sanity checks
            if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
            #endregion

            return Entries.FirstOrDefault(entry => entry.InterfaceUri == interfaceUri);
        }

        /// <summary>
        /// Returns all <see cref="AppEntry"/>s that match a specific search query.
        /// </summary>
        /// <param name="query">The search query. Must be contained within <see cref="AppEntry.Name"/>.</param>
        /// <returns>All <see cref="AppEntry"/>s matching <paramref name="query"/>.</returns>
        public IEnumerable<AppEntry> Search(string? query)
        {
            if (string.IsNullOrEmpty(query))
            {
                foreach (var entry in Entries)
                    yield return entry;
            }
            else
            {
                foreach (var entry in Entries.Where(x => x.InterfaceUri != null && !string.IsNullOrEmpty(x.Name)))
                {
                    if (entry.Name.ContainsIgnoreCase(query)) yield return entry;
                    else if (entry.Name.Replace(' ', '-').ContainsIgnoreCase(query)) yield return entry;
                }
            }
        }

        /// <summary>
        /// Retrieves a specific <see cref="AppAlias"/>.
        /// </summary>
        /// <param name="aliasName">The name of the alias to search for.</param>
        /// <returns>The first <see cref="AppAlias"/> matching <paramref name="aliasName"/> and the <see cref="AppEntry"/> containing it; <c>null</c> if none was found.</returns>
        public (AppAlias alias, AppEntry appEntry)? FindAppAlias(string aliasName)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(aliasName)) throw new ArgumentNullException(nameof(aliasName));
            #endregion

            foreach (var appEntry in Entries)
            {
                if (appEntry.AccessPoints == null) continue;
                foreach (var alias in appEntry.AccessPoints.Entries.OfType<AppAlias>())
                {
                    if (alias.Name == aliasName)
                        return (alias, appEntry);
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the target URI of a specific <see cref="AppAlias"/>.
        /// </summary>
        /// <param name="aliasName">The name of the alias to search for.</param>
        /// <returns>The target feed of the alias; <c>null</c> if none was found.</returns>
        public FeedUri? ResolveAlias(string aliasName)
            => FindAppAlias(aliasName ?? throw new ArgumentNullException(nameof(aliasName)))?.appEntry.InterfaceUri;

        #region Storage
        /// <summary>
        /// Returns the default file path used to store the main <see cref="AppList"/> on this system.
        /// </summary>
        /// <param name="machineWide">Store the <see cref="AppList"/> machine-wide instead of just for the current user.</param>
        public static string GetDefaultPath(bool machineWide = false)
            => Path.Combine(
                machineWide
                    ? Locations.GetIntegrationDirPath("0install.net", true, "desktop-integration") // Machine-wide storage cannot be portable
                    : Locations.GetSaveConfigPath("0install.net", false, "desktop-integration"), // Per-user storage can be portable
                "app-list.xml");

        /// <summary>
        /// Tries to load the <see cref="AppList"/> from its default location. Automatically falls back to an empty list on errors.
        /// </summary>
        /// <param name="machineWide">Load the machine-wide <see cref="AppList"/> instead of the one for the current user.</param>
        /// <returns>The loaded <see cref="AppList"/>.</returns>
        public static AppList LoadSafe(bool machineWide = false)
        {
            try
            {
                return XmlStorage.LoadXml<AppList>(GetDefaultPath(machineWide));
            }
            #region Error handling
            catch (FileNotFoundException)
            {
                return new AppList();
            }
            catch (IOException ex)
            {
                Log.Warn(ex);
                return new AppList();
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn(ex);
                return new AppList();
            }
            catch (InvalidDataException ex)
            {
                Log.Warn(ex);
                return new AppList();
            }
            #endregion
        }

        /// <summary>
        /// Loads a list from an XML file embedded in a ZIP archive.
        /// </summary>
        /// <param name="stream">The ZIP archive to load.</param>
        /// <param name="password">The password to use for decryption; <c>null</c> for no encryption.</param>
        /// <returns>The loaded list.</returns>
        /// <exception cref="ZipException">A problem occurred while reading the ZIP data or <paramref name="password"/> is wrong.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
        public static AppList LoadXmlZip(Stream stream, string? password = null)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            using var zipFile = new ZipFile(stream) {IsStreamOwner = false, Password = password};
            var zipEntry = zipFile.Cast<ZipEntry>().First(x => StringUtils.EqualsIgnoreCase(x.Name, "data.xml"));

            try
            {
                return XmlStorage.LoadXml<AppList>(zipFile.GetInputStream(zipEntry));
            }
            catch (InvalidOperationException)
            {
                throw new InvalidDataException(Resources.SyncServerDataDamaged);
            }
        }

        /// <summary>
        /// Saves the list in an XML file embedded in a ZIP archive.
        /// </summary>
        /// <param name="stream">The ZIP archive to be written.</param>
        /// <param name="password">The password to use for encryption; <c>null</c> for no encryption.</param>
        public void SaveXmlZip(Stream stream, string? password = null)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            if (stream.CanSeek) stream.Position = 0;
            using var zipStream = new ZipOutputStream(stream) {IsStreamOwner = false};
            if (!string.IsNullOrEmpty(password)) zipStream.Password = password;

            // Write the XML file to the ZIP archive
            {
                var entry = new ZipEntry("data.xml") {DateTime = DateTime.Now};
                if (!string.IsNullOrEmpty(password)) entry.AESKeySize = 128;
                zipStream.SetLevel(9);
                zipStream.PutNextEntry(entry);
                this.SaveXml(zipStream);
                zipStream.CloseEntry();
            }
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="AppList"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="AppList"/>.</returns>
        public AppList Clone()
        {
            var appList = new AppList {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
            appList.Entries.AddRange(Entries.CloneElements());

            return appList;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AppList? other)
            => other != null && (base.Equals(other) && Entries.SequencedEquals(other.Entries));

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is AppList list && Equals(list);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Entries.GetUnsequencedHashCode());
        #endregion
    }
}
