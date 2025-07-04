// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Makes an application a browser native messaging host.
/// </summary>
/// <seealso cref="Model.Capabilities.BrowserNativeMessaging"/>
[XmlType("native-messaging", Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class BrowserNativeMessaging : DefaultAccessPoint
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
    {
        var capability = appEntry.LookupCapability<Model.Capabilities.BrowserNativeMessaging>(Capability);
        return [$"native-messaging:{capability.Browser}:{capability.Name}"];
    }

    /// <inheritdoc/>
    public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
    {
        var capability = appEntry.LookupCapability<Model.Capabilities.BrowserNativeMessaging>(Capability);
        var target = new FeedTarget(appEntry.InterfaceUri, feed);

        if (WindowsUtils.IsWindows) Windows.BrowserNativeMessaging.Register(target, capability, iconStore, machineWide);
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        var capability = appEntry.LookupCapability<Model.Capabilities.BrowserNativeMessaging>(Capability);
        if (WindowsUtils.IsWindows) Windows.BrowserNativeMessaging.Unregister(capability, machineWide);
    }

    /// <summary>
    /// Returns the access point in the form "BrowserNativeMessaging: Capability". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"BrowserNativeMessaging: {Capability}";

    /// <inheritdoc/>
    public override AccessPoint Clone() => new BrowserNativeMessaging {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Capability = Capability};
}
