// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Adds a way to explicitly launch the application to the desktop environment.
/// </summary>
/// <seealso cref="Model.Command"/>
[XmlType("command-access-point", Namespace = AppList.XmlNamespace)]
[Equatable, Cloneable]
public abstract partial class CommandAccessPoint : AccessPoint
{
    /// <summary>
    /// The name of the menu entry, icon, command-line, etc..
    /// </summary>
    [Description("The name of the menu entry, icon, command-line, etc..")]
    [XmlAttribute("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The name of the <see cref="Model.Command"/> to use when launching via this access point. Leave empty to use default.
    /// </summary>
    [Description("The name of the Command to use when launching via this access point. Leave empty to use default.")]
    [XmlAttribute("command")]
    public string? Command { get; set; }

    /// <summary>
    /// Ensures that <see cref="Name"/> can safely be used to locate an existing access point. Call from <see cref="AccessPoint.Unapply"/>.
    /// </summary>
    /// <remarks>More lenient than <see cref="SafeName.Validate"/>, so that access points created under older or other rules can still be removed.</remarks>
    /// <exception cref="IOException"><see cref="Name"/> is empty or contains characters not allowed in file names on this platform.</exception>
    [MemberNotNull(nameof(Name))]
    protected void ValidateNameForRemoval()
    {
        if (string.IsNullOrEmpty(Name) || Name is "." or ".." || Name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            throw new IOException(string.Format(Resources.NameInvalidChars, Name));
    }

    #region Conversion
    /// <summary>
    /// Returns the access point in the form "AccessPointType: Name (Command)". Not safe for parsing!
    /// </summary>
    public override string ToString()
    {
        string result = $"{GetType().Name}: {Name}";
        if (!string.IsNullOrEmpty(Command)) result += $" ({Command})";
        return result;
    }
    #endregion
}
