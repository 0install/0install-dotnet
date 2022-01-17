// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Wraps a <see cref="DefaultProgram"/> for data binding.
/// </summary>
public class DefaultProgramModel : IconCapabilityModel
{
    private readonly DefaultProgram _defaultProgram;

    /// <summary>
    /// Returns <see cref="DefaultProgram.Service"/>.
    /// </summary>
    public string Service => _defaultProgram.Service;

    /// <inheritdoc/>
    public DefaultProgramModel(DefaultProgram capability, bool used)
        : base(capability, used)
    {
        _defaultProgram = capability;
    }
}
