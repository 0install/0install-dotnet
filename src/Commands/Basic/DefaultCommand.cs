// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Info;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// The default command used when no command is explicitly specified.
/// </summary>
public class DefaultCommand : CliCommand
{
    public override string Description
    {
        get
        {
            var builder = new StringBuilder(string.Format(Resources.TryHelpWith, "--help") + Environment.NewLine);
            foreach (string possibleCommand in Names)
                builder.AppendLine("0install " + possibleCommand);
            return builder.ToString();
        }
    }

    public override string Usage => "COMMAND";
    protected override int AdditionalArgsMax => 0;

    /// <inheritdoc/>
    public DefaultCommand(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("V|version", () => Resources.OptionVersion, _ =>
        {
            Handler.Output(Resources.VersionInformation,
#if NETFRAMEWORK
                @"Zero Install (.NET Framework) "
#else
                @"Zero Install (.NET) "
#endif
              + ZeroInstallInstance.Version
              + (Locations.IsPortable ? " - " + Resources.PortableMode : "") + Environment.NewLine
              + AppInfo.Current.Copyright + Environment.NewLine
              + Resources.LicenseInfo);
            throw new OperationCanceledException(); // Don't handle any of the other arguments
        });
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        Handler.Output(Resources.CommandLineArguments, HelpText);
        return ExitCode.InvalidArguments;
    }
}
