// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Abstract base class for XML serializable classes that are part of the Zero Install feed model.
/// </summary>
/// <remarks>Does not include <see cref="ZeroInstall.Model.Capabilities"/>.</remarks>
[Equatable]
public abstract partial class FeedElement : XmlUnknown
{
    /// <summary>
    /// Only process this element if the current Zero Install version matches the range.
    /// </summary>
    [Browsable(false)]
    [XmlIgnore]
    public VersionRange? IfZeroInstallVersion { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="IfZeroInstallVersion"/>
    [XmlAttribute("if-0install-version"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? IfZeroInstallVersionString { get => IfZeroInstallVersion?.ToString(); set => IfZeroInstallVersion = string.IsNullOrEmpty(value) ? null : new VersionRange(value); }
    #endregion

    #region Filter
    /// <summary>
    /// Checks whether an element passes the specified <see cref="IfZeroInstallVersion"/> restriction, if any.
    /// </summary>
    protected internal static bool FilterMismatch<T>(T element)
        where T : FeedElement?
    {
        if (element == null) return false;

        return element.IfZeroInstallVersion != null && !element.IfZeroInstallVersion.Match(ModelUtils.Version);
    }

    /// <summary>
    /// Checks whether an element passes the specified <see cref="IfZeroInstallVersion"/> restriction, if any.
    /// </summary>
    protected static bool FilterMismatch(IRecipeStep step) => FilterMismatch(step as FeedElement);
    #endregion
}
