// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Opens the central graphical user interface for launching and managing applications.
/// </summary>
public class Central : CliCommand
{
    public const string Name = "central";
    public override string Description => Resources.DescriptionCentral;
    public override string Usage => "[OPTIONS] [URI]";
    protected override int AdditionalArgsMax => 1;

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

        var startInfo = ProcessUtils.Assembly(
            "ZeroInstall",
            AdditionalArgs is [var uri]
                ? [GetCanonicalUri(uri).ToStringRfc()]
                : _machineWide
                    ? ["--machine"]
                    : []);
        return (ExitCode)startInfo.Run();
    }
}
