// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Globalization;
using System.Linq;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Wraps a <see cref="ContextMenu"/> for data binding.
/// </summary>
public class ContextMenuModel : IconCapabilityModel
{
    private readonly ContextMenu _contextMenu;

    /// <summary>
    /// The name of the first entry in <see cref="VerbCapability.Verbs"/>.
    /// </summary>
    public string? Name
    {
        get
        {
            var verb = _contextMenu.Verbs.FirstOrDefault();
            return verb?.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture)
                ?? verb?.Name;
        }
    }

    /// <inheritdoc/>
    public ContextMenuModel(ContextMenu contextMenu, bool used)
        : base(contextMenu, used)
    {
        _contextMenu = contextMenu;
    }
}
