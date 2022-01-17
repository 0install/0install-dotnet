// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Wraps a <see cref="DefaultCapability"/> for data binding.
/// </summary>
public abstract class CapabilityModel
{
    /// <summary>
    /// The wrapped <see cref="Capability" />.
    /// </summary>
    [Browsable(false)]
    public DefaultCapability Capability { get; }

    /// <summary>
    /// Stores whether the <see cref="CapabilityModel.Capability" /> was already used or not.
    /// </summary>
    private readonly bool _wasUsed;

    /// <summary>
    /// Indicates whether the <see cref="CapabilityModel.Capability" /> shall be used or not.
    /// </summary>
    public bool Use { get; set; }

    /// <summary>
    /// Indicates whether the <see cref="Use" /> of the <see cref="CapabilityModel.Capability" /> has been changed.
    /// </summary>
    [Browsable(false)]
    public bool Changed => (_wasUsed != Use);

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="capability">That shall be wrapped.</param>
    /// <param name="used">Indicates whether the <see cref="Capability" /> was already used.</param>
    protected CapabilityModel(DefaultCapability capability, bool used)
    {
        Capability = capability ?? throw new ArgumentNullException(nameof(capability));
        _wasUsed = Use = used;
    }
}
