// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;

namespace ZeroInstall.Model;

/// <summary>
/// Represents a retrieval method that downloads data from the net.
/// </summary>
[XmlType("download-retrieval-method", Namespace = Feed.XmlNamespace)]
[Equatable]
public abstract partial class DownloadRetrievalMethod : RetrievalMethod, IRecipeStep
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
    [XmlAttribute("href"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
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

        EnsureAttribute(Href, "href");
        Href = ModelUtils.GetAbsoluteHref(Href, feedUri);

        if (Size < 0) throw new InvalidDataException(string.Format(Resources.InvalidXmlAttributeOnTag, "size", ToShortXml()));
    }
    #endregion

    #region Clone
    /// <inheritdoc/>
    IRecipeStep ICloneable<IRecipeStep>.Clone() => (IRecipeStep)Clone();
    #endregion
}
