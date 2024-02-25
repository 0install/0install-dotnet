// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Basic;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Create an alias for a <see cref="Run"/> command.
/// </summary>
public class AddAlias : AppCommand
{
    public const string Name = "alias";
    public const string AltName = "add-alias";
    public override string Usage => "ALIAS [INTERFACE [COMMAND]]";
    public override string Description => Resources.DescriptionAddAlias;
    protected override int AdditionalArgsMax => 3;

    private bool _resolve;
    private bool _remove;

    /// <inheritdoc/>
    public AddAlias(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);

        Options.Add("resolve", () => Resources.OptionAliasResolve, _ => _resolve = true);
        Options.Add("remove", () => Resources.OptionAliasRemove, _ => _remove = true);
    }

    /// <inheritdoc />
    protected override ExitCode ExecuteHelper()
    {
        switch (AdditionalArgs)
        {
            case [var aliasName] when _resolve:
            {
                if (IntegrationManager.AppList.FindAppAlias(aliasName) is not var (alias, appEntry))
                {
                    Handler.Output(Resources.AppAlias, string.Format(Resources.AliasNotFound, aliasName));
                    return ExitCode.IOError;
                }

                string result = appEntry.InterfaceUri.ToStringRfc();
                if (!string.IsNullOrEmpty(alias.Command)) result += $"{Environment.NewLine}Command: {alias.Command}";
                Handler.OutputLow(Resources.AppAlias, result);
                return ExitCode.OK;
            }

            case [var aliasName] when _remove:
            {
                if (IntegrationManager.AppList.FindAppAlias(aliasName) is not var (alias, appEntry))
                {
                    Handler.OutputLow(Resources.AppAlias, string.Format(Resources.AliasNotFound, aliasName));
                    return ExitCode.NoChanges;
                }

                IntegrationManager.RemoveAccessPoints(appEntry, [alias]);
                Handler.OutputLow(Resources.AppAlias, string.Format(Resources.AliasRemoved, aliasName, appEntry.Name));
                return ExitCode.OK;
            }

            case [_, var arg, ..] when _resolve || _remove:
                throw new OptionException(Resources.TooManyArguments + Environment.NewLine + arg.EscapeArgument(), null);

            case [var aliasName, _]:
                CreateAlias(GetAppEntry(IntegrationManager, ref InterfaceUri), aliasName);
                return ExitCode.OK;

            case [var aliasName, _, var command]:
                CreateAlias(GetAppEntry(IntegrationManager, ref InterfaceUri), aliasName, command);
                return ExitCode.OK;

            default:
                throw new OptionException(Resources.MissingArguments, null);
        }
    }
}
