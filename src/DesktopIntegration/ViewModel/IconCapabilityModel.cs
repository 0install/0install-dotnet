// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Globalization;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel
{
    /// <summary>
    /// Wraps an <see cref="IconCapability"/> for data binding.
    /// </summary>
    public class IconCapabilityModel : CapabilityModel
    {
        private readonly IconCapability _iconCapability;

        /// <summary>
        /// Returns the description of the <see cref="IconCapability" /> dependant on <see cref="CultureInfo.CurrentUICulture" />.
        /// </summary>
        public string Description => _iconCapability.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? _iconCapability.ID;

        /// <inheritdoc/>
        protected IconCapabilityModel(IconCapability capability, bool used)
            : base(capability, used)
        {
            _iconCapability = capability;
        }
    }
}
