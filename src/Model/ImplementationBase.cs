// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Model
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
        public string ID { get; set; } = default!;

        /// <summary>
        /// If the feed file is a local file (the interface 'uri' starts with /) then the local-path attribute may contain the pathname of a local directory (either an absolute path or a path relative to the directory containing the feed file).
        /// </summary>
        [Category("Identity"), Description("If the feed file is a local file (the interface 'uri' starts with /) then the local-path attribute may contain the pathname of a local directory (either an absolute path or a path relative to the directory containing the feed file).")]
        [XmlAttribute("local-path"), DefaultValue("")]
        public string? LocalPath { get; set; }

        /// <inheritdoc/>
#pragma warning disable 8765
        [XmlIgnore]
        public override ImplementationVersion Version { get; set; } = default!;
#pragma warning restore 8765

        private ManifestDigest _manifestDigest;

        /// <summary>
        /// A manifest digest is a means of uniquely identifying an <see cref="Implementation"/> and verifying its contents.
        /// </summary>
        [Category("Identity"), Description("A manifest digest is a means of uniquely identifying an Implementation and verifying its contents.")]
        [XmlElement("manifest-digest")]
        public ManifestDigest ManifestDigest { get => _manifestDigest; set => _manifestDigest = value; }

        #region Normalize
        /// <inheritdoc/>
        public override void Normalize(FeedUri? feedUri = null)
        {
            base.Normalize(feedUri);

            // Merge the version modifier into the normal version attribute
            if (!string.IsNullOrEmpty(VersionModifier))
            {
                Version = new(Version + VersionModifier);
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

            EnsureTag(ID, "id");
            EnsureTag(Version, "version");
        }
        #endregion

        #region Clone
        /// <summary>
        /// Copies all known values from one instance to another. Helper method for instance cloning.
        /// </summary>
        protected static void CloneFromTo(ImplementationBase from, ImplementationBase to)
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
            => StringUtils.Join(", ", new object?[]
                {
                    ID,
                    Architecture,
                    Version,
                    Released.ToString("d", CultureInfo.InvariantCulture),
                    ReleasedVerbatim,
                    Stability,
                    License,
                    Main
                }.WhereNotNull()
                 .Where(x => x is not 0)
                 .Select(x => x.ToString() ?? ""));
        #endregion

        #region Equality
        protected bool Equals(ImplementationBase? other)
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
