// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Contains a list of <see cref="Feed"/>s, reduced to only contain information relevant for overview lists.
    /// For specific <see cref="Implementation"/>s, fetch the original <see cref="Feed"/>s.
    /// Catalogs downloaded from remote locations are protected from tampering by a OpenPGP signature.
    /// </summary>
    [Description("Contains a list of feeds, reduced to only contain information relevant for overview lists.")]
    [Serializable, XmlRoot("catalog", Namespace = XmlNamespace), XmlType("catalog", Namespace = XmlNamespace)]
    [XmlNamespace("xsi", XmlStorage.XsiNamespace)]
    //[XmlNamespace("feed", Feed.XmlNamespace)]
    public class Catalog : XmlUnknown, ICloneable<Catalog>, IEquatable<Catalog>
    {
        #region Constants
        /// <summary>
        /// The XML namespace used for storing feed catalogs. Used in combination with <see cref="Feed.XmlNamespace"/>.
        /// </summary>
        public const string XmlNamespace = "http://0install.de/schema/injector/catalog";

        /// <summary>
        /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
        /// </summary>
        public const string XsdLocation = XmlNamespace + "/catalog.xsd";

        /// <summary>
        /// Provides XML Editors with location hints for XSD files.
        /// </summary>
        public const string XsiSchemaLocation = XmlNamespace + " " + XsdLocation;

        /// <summary>
        /// Provides XML Editors with location hints for XSD files.
        /// </summary>
        [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
        public string SchemaLocation = XsiSchemaLocation;
        #endregion

        /// <summary>
        /// A list of <see cref="Feed"/>s contained within this catalog.
        /// </summary>
        [Browsable(false)]
        [XmlElement("interface", typeof(Feed), Namespace = Feed.XmlNamespace)]
        public List<Feed> Feeds { get; } = new List<Feed>();

        /// <summary>
        /// Determines whether this catalog contains a <see cref="Feed"/> with a specific URI.
        /// </summary>
        /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
        /// <returns><c>true</c> if a matching feed was found; <c>false</c> otherwise.</returns>
        public bool ContainsFeed(FeedUri uri)
        {
            #region Sanity checks
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            #endregion

            return Feeds.Any(feed => feed.Uri == uri);
        }

        /// <summary>
        /// Returns the <see cref="Feed"/> with a specific URI.
        /// </summary>
        /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
        /// <returns>The identified <see cref="Feed"/>.</returns>
        /// <exception cref="KeyNotFoundException">No <see cref="Feed"/> matching <paramref name="uri"/> was found in <see cref="Feeds"/>.</exception>
        public Feed this[FeedUri uri]
        {
            get
            {
                #region Sanity checks
                if (uri == null) throw new ArgumentNullException(nameof(uri));
                #endregion

                try
                {
                    return Feeds.First(feed => feed.Uri == uri);
                }
                #region Error handling
                catch (InvalidOperationException)
                {
                    throw new KeyNotFoundException(string.Format(Resources.FeedNotInCatalog, uri));
                }
                #endregion
            }
        }

        /// <summary>
        /// Returns the <see cref="Feed"/> with a specific URI. Safe for missing elements.
        /// </summary>
        /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
        /// <returns>The identified <see cref="Feed"/>; <c>null</c> if no matching entry was found.</returns>
        public Feed? GetFeed(FeedUri uri)
        {
            #region Sanity checks
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            #endregion

            return Feeds.FirstOrDefault(feed => feed.Uri == uri);
        }

        /// <summary>
        /// Returns the first <see cref="Feed"/> that matches a specific short name.
        /// </summary>
        /// <param name="shortName">The short name to look for. Must match either <see cref="Feed.Name"/> or <see cref="EntryPoint.BinaryName"/> of <see cref="Command.NameRun"/>.</param>
        /// <returns>The first matching <see cref="Feed"/>; <c>null</c> if no match was found.</returns>
        public Feed? FindByShortName(string? shortName)
        {
            if (string.IsNullOrEmpty(shortName)) return null;

            foreach (var feed in Feeds.Where(x => x.Uri != null && !string.IsNullOrEmpty(x.Name)))
            {
                if (StringUtils.EqualsIgnoreCase(feed.Name, shortName)) return feed;
                if (StringUtils.EqualsIgnoreCase(feed.Name.Replace(' ', '-'), shortName)) return feed;

                var entryPoint = feed.GetEntryPoint();
                if (!string.IsNullOrEmpty(entryPoint?.BinaryName) && StringUtils.EqualsIgnoreCase(entryPoint.BinaryName, shortName))
                    return feed;
            }

            return null;
        }

        /// <summary>
        /// Returns all <see cref="Feed"/>s that match a specific search query.
        /// </summary>
        /// <param name="query">The search query. Must be contained within <see cref="Feed.Name"/> or <see cref="EntryPoint.BinaryName"/> of <see cref="Command.NameRun"/>.</param>
        /// <returns>All <see cref="Feed"/>s matching <paramref name="query"/>.</returns>
        public IEnumerable<Feed> Search(string? query)
        {
            if (string.IsNullOrEmpty(query))
            {
                foreach (var feed in Feeds)
                    yield return feed;
            }
            else
            {
                foreach (var feed in Feeds)
                {
                    if (feed.Uri != null && !string.IsNullOrEmpty(feed.Name))
                    {
                        if (feed.Name.ContainsIgnoreCase(query)) yield return feed;
                        else if (feed.Name.Replace(' ', '-').ContainsIgnoreCase(query)) yield return feed;
                    }
                }
            }
        }

        #region Normalize
        /// <summary>
        /// Runs <see cref="Feed.Normalize"/> on all contained <see cref="Feeds"/>.
        /// </summary>
        /// <remarks>This method should be called to prepare a <see cref="Catalog"/> for solver processing. Do not call it if you plan on serializing the catalog again since it may loose some of its structure.</remarks>
        public void Normalize()
        {
            foreach (var feed in Feeds)
                feed.Normalize(feed.Uri);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Catalog"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Catalog"/>.</returns>
        public Catalog Clone()
        {
            var catalog = new Catalog {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
            catalog.Feeds.AddRange(Feeds.CloneElements());
            return catalog;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Catalog other)
            => other != null && base.Equals(other) && Feeds.SequencedEquals(other.Feeds);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(Catalog) && Equals((Catalog)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Feeds.GetSequencedHashCode());
        #endregion
    }
}
