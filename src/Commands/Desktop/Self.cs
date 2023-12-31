// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Manages the integration of Zero Install itself in the operating system (deployment and removal).
/// </summary>
public sealed partial class Self(ICommandHandler handler) : CliMultiCommand(handler)
{
    public const string Name = "self";
    public const string AltName = "maintenance";

    /// <inheritdoc/>
    public override IEnumerable<string> SubCommandNames => [Deploy.Name, Remove.Name, Update.Name];

    /// <inheritdoc/>
    public override CliCommand GetCommand(string commandName)
        => (commandName ?? throw new ArgumentNullException(nameof(commandName))) switch
        {
            Deploy.Name => new Deploy(Handler),
            Remove.Name => new Remove(Handler),
            RemoveHelper.Name => new RemoveHelper(Handler),
            Update.Name => new Update(Handler),
            _ => throw new OptionException(string.Format(Resources.UnknownCommand, commandName), commandName)
        };

    public abstract class SelfSubCommand : CliCommand, ICliSubCommand
    {
        public string ParentName => Name;

        protected SelfSubCommand(ICommandHandler handler)
            : base(handler)
        {}
    }
}
