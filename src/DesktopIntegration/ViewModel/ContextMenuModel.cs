// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Globalization;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel
{
    /// <summary>
    /// Wraps a <see cref="ContextMenu"/> for data binding.
    /// </summary>
    public class ContextMenuModel : CapabilityModel
    {
        private readonly ContextMenu _contextMenu;

        /// <summary>
        /// The name of the stored <see cref="ContextMenu.Verb"/>.
        /// </summary>
        public string Name => _contextMenu.Verb.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? _contextMenu.Verb.Name;

        /// <inheritdoc/>
        public ContextMenuModel(ContextMenu contextMenu, bool used)
            : base(contextMenu, used)
        {
            _contextMenu = contextMenu;
        }
    }
}
