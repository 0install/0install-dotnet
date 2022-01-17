// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Wraps a <see cref="AutoPlay"/> for data binding.
/// </summary>
public class AutoPlayModel : IconCapabilityModel
{
    private readonly AutoPlay _autoPlay;

    /// <summary>
    /// All <see cref="AutoPlay.Events"/> concatenated with ", ".
    /// </summary>
    public string Events => StringUtils.Join(", ", _autoPlay.Events.Select(x => x.Name));

    /// <inheritdoc/>
    public AutoPlayModel(AutoPlay capability, bool used)
        : base(capability, used)
    {
        _autoPlay = capability;
    }
}
