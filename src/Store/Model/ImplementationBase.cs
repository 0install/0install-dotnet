// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Information for identifying an implementation of a <see cref="Feed"/>.
    /// Common base for <see cref="Implementation"/> and <see cref="ImplementationSelection"/>.
    /// </summary>
    [XmlType("implementation-base", Namespace = Feed.XmlNamespace)]
    public abstract class ImplementationBase : Element
    {
        /// <summary>
        /// A unique identifier for this implementation. Used when storing implementation-specific user preferences.
        /// </summary>
        [Category("Identity"), Description("A unique identifier for this implementation. Used when storing implementation-specific user preferences.")]
        [XmlAttribute("id"), DefaultValue("")]
        public string ID { get; set; }

        /// <summary>
        /// If the feed file is a local file (the interface 'uri' starts with /) then the local-path attribute may contain the pathname of a local directory (either an absolute path or a path relative to the directory containing the feed file).
        /// </summary>
        [Category("Identity"), Description("If the feed file is a local file (the interface 'uri' starts with /) then the local-path attribute may contain the pathname of a local directory (either an absolute path or a path relative to the directory containing the feed file).")]
        [XmlAttribute("local-path"), DefaultValue("")]
        public string LocalPath { get; set; }

        private ManifestDigest _manifestDigest;

        /// <summary>
        /// A manifest digest is a means of uniquely identifying an <see cref="Implementation"/> and verifying its contents.
        /// </summary>
        [Category("Identity"), Description("A manifest digest is a means of uniquely identifying an Implementation and verifying its contents.")]
        [XmlElement("manifest-digest")]
        public ManifestDigest ManifestDigest { get => _manifestDigest; set => _manifestDigest = value; }

        #region Normalize
        /// <summary>
        /// Sets missing default values and handles legacy elements.
        /// </summary>
        /// <param name="feedUri">The feed the data was originally loaded from.</param>
        /// <remarks>This method should be called to prepare a <see cref="Feed"/> for solver processing. Do not call it if you plan on serializing the feed again since it may loose some of its structure.</remarks>
        public override void Normalize(FeedUri feedUri = null)
        {
            base.Normalize(feedUri);

            // Merge the version modifier into the normal version attribute
            if (!string.IsNullOrEmpty(VersionModifier))
            {
                Version = new ImplementationVersion(Version + VersionModifier);
                VersionModifier = null;
            }

            // Default stability rating to testing
            if (Stability == Stability.Unset) Stability = Stability.Testing;

            // Make local paths absolute
            try
            {
                if (!string.IsNullOrEmpty(LocalPath))
                    LocalPath = ModelUtils.GetAbsolutePath(LocalPath, feedUri);
                else if (!string.IsNullOrEmpty(ID) && ID.StartsWith(".")) // Get local path from ID
                    LocalPath = ID = ModelUtils.GetAbsolutePath(ID, feedUri);
            }
            #region Error handling
            catch (UriFormatException ex)
            {
                Log.Error(ex);
                LocalPath = null;
            }
            #endregion

            // Parse manifest digest from ID if missing
            if (!string.IsNullOrEmpty(ID)) _manifestDigest.ParseID(ID);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Copies all known values from one instance to another. Helper method for instance cloning.
        /// </summary>
        protected static void CloneFromTo([NotNull] ImplementationBase from, [NotNull] ImplementationBase to)
        {
            Element.CloneFromTo(from ?? throw new ArgumentNullException(nameof(from)), to ?? throw new ArgumentNullException(nameof(to)));
            to.ID = from.ID;
            to.LocalPath = from.LocalPath;
            to.ManifestDigest = from.ManifestDigest;
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the implementation in the form "Comma-separated list of set values". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(ID)) parts.Add(ID);
            if (Architecture != default) parts.Add(Architecture.ToString());
            if (Version != null) parts.Add(Version.ToString());
            if (Released != default) parts.Add(Released.ToString("d", CultureInfo.InvariantCulture));
            if (ReleasedVerbatim != null) parts.Add(ReleasedVerbatim);
            if (Stability != default) parts.Add(Stability.ToString());
            if (!string.IsNullOrEmpty(License)) parts.Add(License);
            if (!string.IsNullOrEmpty(Main)) parts.Add(Main);
            return StringUtils.Join(", ", parts);
        }
        #endregion

        #region Equality
        protected bool Equals(ImplementationBase other)
            => other != null
            && base.Equals(other)
            && other.ID == ID
            && other.LocalPath == LocalPath
            && other.ManifestDigest == ManifestDigest;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                ID,
                LocalPath,
                ManifestDigest);
        #endregion
    }
}
