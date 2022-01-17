// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model;

/// <summary>
/// An object that has localizable summaries and descriptions.
/// </summary>
public interface ISummaryContainer : IDescriptionContainer
{
    /// <summary>
    /// Short one-line descriptions for different languages; the first word should not be upper-case unless it is a proper noun (e.g. "cures all ills").
    /// </summary>
    [Browsable(false)]
    [XmlElement("summary")]
    LocalizableStringCollection Summaries { get; }
}
