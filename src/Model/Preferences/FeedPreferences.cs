// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Model.Properties;

namespace ZeroInstall.Model.Preferences
{
    /// <summary>
    /// Stores user-specific preferences for a <see cref="Feed"/>.
    /// </summary>
    [XmlRoot("feed-preferences", Namespace = Feed.XmlNamespace), XmlType("feed-preferences", Namespace = Feed.XmlNamespace)]
    public sealed class FeedPreferences : XmlUnknown, ICloneable<FeedPreferences>, IEquatable<FeedPreferences>
    {
        /// <summary>
        /// The point in time this feed was last checked for updates.
        /// </summary>
        [Description("The point in time this feed was last checked for updates.")]
        [XmlIgnore]
        public DateTime LastChecked { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="LastChecked"/>
        [XmlAttribute("last-checked"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long LastCheckedUnix { get => LastChecked.ToUnixTime(); set => LastChecked = FileUtils.FromUnixTime(value); }
        #endregion

        /// <summary>
        /// A list of implementation-specific user-overrides.
        /// </summary>
        [Description("A list of implementation-specific user-overrides.")]
        [XmlElement("implementation")]
        public List<ImplementationPreferences> Implementations { get; } = new();

        /// <summary>
        /// Retrieves an existing entry from <see cref="Implementations"/> by ID or creates a new one if no appropriate one exists.
        /// </summary>
        /// <param name="id">The <see cref="ImplementationPreferences.ID"/> to search for.</param>
        /// <returns>The found or newly created <see cref="ImplementationPreferences"/>.</returns>
        public ImplementationPreferences this[string id]
        {
            get
            {
                var result = Implementations.FirstOrDefault(implementation => implementation.ID == id);
                if (result == null)
                {
                    result = new ImplementationPreferences {ID = id};
                    Implementations.Add(result);
                }
                return result;
            }
        }

        #region Normalize
        /// <summary>
        /// Removes superfluous entries from <see cref="Implementations"/>.
        /// </summary>
        public void Normalize() => Implementations.RemoveAll(implementation => implementation.IsSuperfluous);
        #endregion

        #region Storage
        /// <summary>
        /// Loads <see cref="FeedPreferences"/> for a specific feed.
        /// </summary>
        /// <param name="feedUri">The feed to load the preferences for.</param>
        /// <returns>The loaded <see cref="FeedPreferences"/>.</returns>
        /// <exception cref="IOException">A problem occurred while reading the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
        public static FeedPreferences LoadFor(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            string? path = Locations.GetLoadConfigPaths("0install.net", true, "injector", "feeds", feedUri.PrettyEscape()).FirstOrDefault();
            if (string.IsNullOrEmpty(path)) return new();

            Log.Debug("Loading feed preferences for " + feedUri.ToStringRfc() + " from: " + path);
            return XmlStorage.LoadXml<FeedPreferences>(path);
        }

        /// <summary>
        /// Tries to load <see cref="FeedPreferences"/> for a specific feed. Automatically falls back to defaults on errors.
        /// </summary>
        /// <param name="feedUri">The feed to load the preferences for.</param>
        /// <returns>The loaded <see cref="FeedPreferences"/> or default value if there was a problem.</returns>
        public static FeedPreferences LoadForSafe(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            try
            {
                return LoadFor(feedUri);
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Warn(string.Format(Resources.ErrorLoadingFeedPrefs, feedUri));
                Log.Warn(ex);
                return new();
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn(string.Format(Resources.ErrorLoadingFeedPrefs, feedUri));
                Log.Warn(ex);
                return new();
            }
            catch (InvalidDataException ex)
            {
                Log.Warn(string.Format(Resources.ErrorLoadingFeedPrefs, feedUri));
                Log.Warn(ex);
                return new();
            }
            #endregion
        }

        /// <summary>
        /// Saves these <see cref="FeedPreferences"/> for a specific feed.
        /// </summary>
        /// <param name="feedUri">The feed to save the preferences for.</param>
        /// <exception cref="IOException">A problem occurred while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
        public void SaveFor(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            Normalize();

            string path = Locations.GetSaveConfigPath("0install.net", true, "injector", "feeds", feedUri.PrettyEscape());

            Log.Debug("Saving feed preferences for " + feedUri.ToStringRfc() + " to: " + path);
            this.SaveXml(path);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="FeedPreferences"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="FeedPreferences"/>.</returns>
        public FeedPreferences Clone()
        {
            var feedPreferences = new FeedPreferences {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, LastChecked = LastChecked};
            feedPreferences.Implementations.AddRange(Implementations.CloneElements());
            return feedPreferences;
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the preferences in the form "FeedPreferences: LastChecked". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"FeedPreferences: {LastChecked}";
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(FeedPreferences? other)
            => other != null
            && base.Equals(other)
            && LastChecked == other.LastChecked
            && Implementations.SequencedEquals(other.Implementations);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is FeedPreferences preferences && Equals(preferences);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
               base.GetHashCode(),
               LastChecked,
               Implementations.GetSequencedHashCode());
        #endregion
    }
}
