// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Abstract base class for capabilities that can have multiple <see cref="Icon"/>s and descriptions.
/// </summary>
[Serializable, XmlType("icon-capability", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public abstract partial class IconCapability : DefaultCapability, IIconContainer, IDescriptionContainer
{
    /// <inheritdoc/>
    [Browsable(false)]
    [XmlElement("description")]
    [OrderedEquality]
    public LocalizableStringCollection Descriptions { get; } = [];

    /// <summary>
    /// Zero or more icons to represent the capability. Used for things like file icons.
    /// </summary>
    [Browsable(false)]
    [XmlElement("icon", Namespace = Feed.XmlNamespace)]
    [OrderedEquality]
    public List<Icon> Icons { get; } = [];

    /// <summary>
    /// Returns the first icon with a specific MIME type.
    /// </summary>
    /// <param name="mimeType">The <see cref="Icon.MimeType"/> to try to find. Will only return exact matches.</param>
    /// <returns>The best matching icon that was found or <c>null</c> if no matching icon was found.</returns>
    public Icon? GetIcon(string mimeType)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
        #endregion

        return Icons.FirstOrDefault(icon => StringUtils.EqualsIgnoreCase(icon.MimeType, mimeType));
    }
}
