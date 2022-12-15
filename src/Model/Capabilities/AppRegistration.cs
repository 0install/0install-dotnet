// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Indicates that an application should be listed in the "Set your Default Programs" UI (Windows Vista and later).
/// </summary>
/// <remarks>The actual integration information is pulled from the other <see cref="Capability"/>s.</remarks>
[Description("""Indicates that an application should be listed in the "Set your Default Programs" UI (Windows Vista and later).""")]
[Serializable, XmlRoot("registration", Namespace = CapabilityList.XmlNamespace), XmlType("registration", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public sealed partial class AppRegistration : Capability
{
    /// <inheritdoc/>
    public override bool WindowsMachineWideOnly => !WindowsUtils.IsWindows8;

    /// <summary>
    /// The registry path relative to HKEY_CURRENT_USER or HKEY_LOCAL_MACHINE which should be used to store the application's capability registration information.
    /// </summary>
    [Description("The registry path relative to HKEY_CURRENT_USER or HKEY_LOCAL_MACHINE which should be used to store the application's capability registration information.")]
    [XmlAttribute("capability-reg-path")]
    public required string CapabilityRegPath { get; set; }

    /// <inheritdoc/>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public override IEnumerable<string> ConflictIDs => new[] {$"registered-apps:{ID}", $"hklm:{CapabilityRegPath}"};

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();
        EnsureAttribute(CapabilityRegPath, "capability-reg-path");
        if (CapabilityRegPath.Contains(".."))
            throw new InvalidDataException($"Invalid 'capability-reg-path' attribute on {ToShortXml()}. Should not contain '..' but was: {CapabilityRegPath}");
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the capability in the form "CapabilityRegPath". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{CapabilityRegPath}";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override Capability Clone() => new AppRegistration {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, CapabilityRegPath = CapabilityRegPath};
    #endregion
}
