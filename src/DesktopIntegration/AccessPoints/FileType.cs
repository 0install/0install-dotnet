// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Makes an application the default handler for a specific file type.
/// </summary>
/// <seealso cref="Model.Capabilities.FileType"/>
[XmlType("file-type", Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class FileType : DefaultAccessPoint
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        var capability = appEntry.LookupCapability<Model.Capabilities.FileType>(Capability);
        return capability.Extensions.Select(extension => $"extension:{extension.Value}");
    }

    /// <inheritdoc/>
    public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        var capability = appEntry.LookupCapability<Model.Capabilities.FileType>(Capability);
        var target = new FeedTarget(appEntry.InterfaceUri, feed);
        if (WindowsUtils.IsWindows) Windows.FileType.Register(target, capability, iconStore, machineWide, accessPoint: true);
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        var capability = appEntry.LookupCapability<Model.Capabilities.FileType>(Capability);
        if (WindowsUtils.IsWindows) Windows.FileType.Unregister(capability, machineWide, accessPoint: true);
    }

    #region Conversion
    /// <summary>
    /// Returns the access point in the form "FileType: Capability". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"FileType: {Capability}";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override AccessPoint Clone() => new FileType {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Capability = Capability};
    #endregion
}
