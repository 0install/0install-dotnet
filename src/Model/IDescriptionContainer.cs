// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// An object that has localizable descriptions.
/// </summary>
public interface IDescriptionContainer
{
    /// <summary>
    /// Full descriptions for different languages, which can be several paragraphs long.
    /// </summary>
    [Browsable(false)]
    [XmlElement("description")]
    LocalizableStringCollection Descriptions { get; }
}
