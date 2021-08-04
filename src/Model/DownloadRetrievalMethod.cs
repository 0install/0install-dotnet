// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Net;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Represents a retrieval method that downloads data from the net.
    /// </summary>
    [XmlType("download-retrieval-method", Namespace = Feed.XmlNamespace)]
    public abstract class DownloadRetrievalMethod : RetrievalMethod, IRecipeStep
    {
        /// <summary>
        /// The URL to download the file from. Relative URLs are only allowed in local feed files.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Uri Href { get; set; } = default!;

        #region XML serialization
        /// <summary>Used for XML serialization and PropertyGrid.</summary>
        /// <seealso cref="Href"/>
        [DisplayName(@"Href"), Description("The URL to download the file from. Relative URLs are only allowed in local feed files.")]
        [XmlAttribute("href"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        // ReSharper disable once ConstantConditionalAccessQualifier
        public string HrefString { get => Href?.ToStringRfc()!; set => Href = new(value, UriKind.RelativeOrAbsolute); }
        #endregion

        /// <summary>
        /// The size of the file in bytes. The file must have the given size or it will be rejected.
        /// </summary>
        [Description("The size of the file in bytes. The file must have the given size or it will be rejected.")]
        [XmlAttribute("size"), DefaultValue(0L)]
        public long Size { get; set; }

        /// <summary>
        /// The effective size of the file on the server.
        /// </summary>
        [XmlIgnore, Browsable(false)]
        public virtual long DownloadSize => Size;

        #region Normalize
        /// <inheritdoc cref="RetrievalMethod.Normalize"/>
        public override void Normalize(FeedUri? feedUri = null)
        {
            base.Normalize(feedUri);

            EnsureTag(Href, "href");
            Href = ModelUtils.GetAbsoluteHref(Href, feedUri);
        }
        #endregion

        #region Clone
        /// <inheritdoc/>
        IRecipeStep ICloneable<IRecipeStep>.Clone() => (IRecipeStep)Clone();
        #endregion

        #region Equality
        protected bool Equals(DownloadRetrievalMethod? other) => other != null && base.Equals(other) && other.Href == Href && other.Size == Size;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Href, Size);
        #endregion
    }
}
