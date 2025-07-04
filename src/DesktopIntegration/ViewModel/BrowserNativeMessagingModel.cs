// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Wraps a <see cref="BrowserNativeMessaging"/> for data binding.
/// </summary>
public class BrowserNativeMessagingModel : CapabilityModel
{
    private readonly BrowserNativeMessaging _nativeMessaging;

    /// <summary>
    /// The browser the native messaging host can be registered in.
    /// </summary>
    public Browser Browser => _nativeMessaging.Browser;

    /// <summary>
    /// The name used to call the native messaging host from browser extensions.
    /// </summary>
    public string Name => _nativeMessaging.Name;

    /// <inheritdoc/>
    public BrowserNativeMessagingModel(BrowserNativeMessaging capability, bool used)
        : base(capability, used)
    {
        _nativeMessaging = capability;
    }
}
