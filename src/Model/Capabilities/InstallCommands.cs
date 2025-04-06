// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using ZeroInstall.Model.Design;
#endif

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Lists the commands the application normally registers for use by Windows' "Set Program Access and Defaults".
/// Used by registry virtualization to stand in for the actual Zero Install commands at runtime.
/// </summary>
/// <param name="Reinstall">The path (relative to the installation directory) to the executable used to set an application as the default program without any arguments.</param>
/// <param name="ReinstallArgs">Additional arguments for the executable specified in <see cref="Reinstall"/>.</param>
/// <param name="ShowIcons">The path (relative to the installation directory) to the executable used to create icons/shortcuts to the application without any arguments.</param>
/// <param name="ShowIconsArgs"> Additional arguments for the executable specified in <see cref="ShowIcons"/>.</param>
/// <param name="HideIcons">The path (relative to the installation directory) to the executable used to remove icons/shortcuts to the application without any arguments.</param>
/// <param name="HideIconsArgs">Additional arguments for the executable specified in <see cref="HideIcons"/>.</param>
[Description("""
Lists the commands the application normally registers for use by Windows' "Set Program Access and Defaults".
Used by registry virtualization to stand in for the actual Zero Install commands at runtime.
""")]
#if !MINIMAL
[TypeConverter(typeof(InstallCommandsConverter))]
#endif
[Serializable, XmlType("install-commands", Namespace = CapabilityList.XmlNamespace)]
public record struct InstallCommands(
    [property: XmlAttribute("reinstall"), DefaultValue(""), Description("The path (relative to the installation directory) to the executable used to set an application as the default program without any arguments.")]
    string? Reinstall,
    [property: XmlAttribute("reinstall-args"), DefaultValue(""), Description("Additional arguments for the executable specified in Reinstall.")]
    string? ReinstallArgs,
    [property: XmlAttribute("show-icons"), DefaultValue(""), Description("The path (relative to the installation directory) to the executable used to create icons/shortcuts to the application without any arguments.")]
    string? ShowIcons,
    [property: XmlAttribute("show-icons-args"), DefaultValue(""), Description("Additional arguments for the executable specified in ShowIcons.")]
    string? ShowIconsArgs,
    [property: XmlAttribute("hide-icons"), DefaultValue(""), Description("The path (relative to the installation directory) to the executable used to remove icons/shortcuts to the application without any arguments.")]
    string? HideIcons,
    [property: XmlAttribute("hide-icons-args"), DefaultValue(""), Description("Additional arguments for the executable specified in HideIcons.")]
    string? HideIconsArgs
);
