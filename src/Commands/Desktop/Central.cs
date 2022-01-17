// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common;
using NanoByte.Common.Native;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Opens the central graphical user interface for launching and managing applications.
/// </summary>
public class Central : CliCommand
{
    public const string Name = "central";
    public override string Description => Resources.DescriptionCentral;
    public override string Usage => "[OPTIONS]";
    protected override int AdditionalArgsMax => 0;

    private bool _machineWide;

    /// <inheritdoc/>
    public Central(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("m|machine", () => Resources.OptionMachine, _ => _machineWide = true);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        if (_machineWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator)
            throw new NotAdminException(Resources.MustBeAdminForMachineWide);

        var process = _machineWide
            ? ProcessUtils.Assembly("ZeroInstall", "--machine")
            : ProcessUtils.Assembly("ZeroInstall");
        return (ExitCode)process.Run();
    }
}
