// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Indicates that all compatible capabilities should be registered.
/// </summary>
/// <seealso cref="ZeroInstall.Model.Capabilities"/>
[XmlType(TagName, Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class CapabilityRegistration : AccessPoint
{
    public const string TagName = "capability-registration", AltName = "capabilities";

    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
        => appEntry.CapabilityLists.CompatibleCapabilities().SelectMany(x => x.ConflictIDs);

    /// <inheritdoc/>
    public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        var target = new FeedTarget(appEntry.InterfaceUri, feed);

        if (WindowsUtils.IsWindows) Windows.UninstallEntry.Register(target, iconStore, machineWide);

        var capabilities = appEntry.CapabilityLists.CompatibleCapabilities().ToList();
        foreach (var capability in capabilities)
        {
            switch (capability)
            {
                case Model.Capabilities.FileType fileType:
                    if (WindowsUtils.IsWindows) Windows.FileType.Register(target, fileType, iconStore, machineWide);
                    else if (UnixUtils.IsUnix) Unix.FileType.Register(target, fileType, iconStore, machineWide);
                    break;

                case Model.Capabilities.UrlProtocol urlProtocol:
                    if (WindowsUtils.IsWindows) Windows.UrlProtocol.Register(target, urlProtocol, iconStore, machineWide);
                    else if (UnixUtils.IsUnix) Unix.UrlProtocol.Register(target, urlProtocol, iconStore, machineWide);
                    break;

                case Model.Capabilities.AutoPlay autoPlay:
                    if (WindowsUtils.IsWindows) Windows.AutoPlay.Register(target, autoPlay, iconStore, machineWide);
                    break;

                case AppRegistration appRegistration:
                    if ((WindowsUtils.IsWindows && machineWide) || WindowsUtils.IsWindows8) Windows.AppRegistration.Register(target, appRegistration, capabilities.OfType<VerbCapability>(), iconStore, machineWide);
                    break;

                case Model.Capabilities.DefaultProgram defaultProgram:
                    if (WindowsUtils.IsWindows && machineWide) Windows.DefaultProgram.Register(target, defaultProgram, iconStore);
                    else if (UnixUtils.IsUnix) Unix.DefaultProgram.Register(target, defaultProgram, iconStore, machineWide);
                    break;

                case ComServer comServer:
                    if (WindowsUtils.IsWindows) Windows.ComServer.Register(target, comServer, iconStore, machineWide);
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        foreach (var capability in appEntry.CapabilityLists.CompatibleCapabilities())
        {
            switch (capability)
            {
                case Model.Capabilities.FileType fileType:
                    if (WindowsUtils.IsWindows) Windows.FileType.Unregister(fileType, machineWide);
                    else if (UnixUtils.IsUnix) Unix.FileType.Unregister(fileType, machineWide);
                    break;

                case Model.Capabilities.UrlProtocol urlProtocol:
                    if (WindowsUtils.IsWindows) Windows.UrlProtocol.Unregister(urlProtocol, machineWide);
                    else if (UnixUtils.IsUnix) Unix.UrlProtocol.Unregister(urlProtocol, machineWide);
                    break;

                case Model.Capabilities.AutoPlay autoPlay:
                    if (WindowsUtils.IsWindows) Windows.AutoPlay.Unregister(autoPlay, machineWide);
                    break;

                case AppRegistration appRegistration:
                    if ((WindowsUtils.IsWindows && machineWide) || WindowsUtils.IsWindows8) Windows.AppRegistration.Unregister(appRegistration, machineWide);
                    break;

                case Model.Capabilities.DefaultProgram defaultProgram:
                    if (WindowsUtils.IsWindows && machineWide) Windows.DefaultProgram.Unregister(defaultProgram);
                    else if (UnixUtils.IsUnix) Unix.DefaultProgram.Unregister(defaultProgram, machineWide);
                    break;

                case ComServer comServer:
                    if (WindowsUtils.IsWindows) Windows.ComServer.Unregister(comServer, machineWide);
                    break;
            }
        }

        if (WindowsUtils.IsWindows) Windows.UninstallEntry.Unregister(appEntry.InterfaceUri, machineWide);
    }

    #region Conversion
    /// <summary>
    /// Returns the access point in the form "CapabilityRegistration". Not safe for parsing!
    /// </summary>
    public override string ToString() => "CapabilityRegistration";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override AccessPoint Clone() => new CapabilityRegistration {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
    #endregion
}
