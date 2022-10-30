// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Makes an application the default handler for a specific URL protocol.
/// </summary>
/// <seealso cref="Model.Capabilities.UrlProtocol"/>
[XmlType("url-protocol", Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class UrlProtocol : DefaultAccessPoint
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        var capability = appEntry.LookupCapability<Model.Capabilities.UrlProtocol>(Capability);
        return capability.KnownPrefixes.Count == 0
            ? new[] {$"protocol:{capability.ID}"}
            : capability.KnownPrefixes.Select(prefix => $"protocol:{prefix.Value}");
    }

    /// <inheritdoc/>
    public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        var capability = appEntry.LookupCapability<Model.Capabilities.UrlProtocol>(Capability);
        var target = new FeedTarget(appEntry.InterfaceUri, feed);
        if (WindowsUtils.IsWindows) Windows.UrlProtocol.Register(target, capability, iconStore, machineWide, accessPoint: true);
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        var capability = appEntry.LookupCapability<Model.Capabilities.UrlProtocol>(Capability);
        if (WindowsUtils.IsWindows) Windows.UrlProtocol.Unregister(capability, machineWide, accessPoint: true);
    }

    #region Conversion
    /// <summary>
    /// Returns the access point in the form "UrlProtocol: Capability". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"UrlProtocol: {Capability}";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override AccessPoint Clone() => new UrlProtocol {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Capability = Capability};
    #endregion
}
