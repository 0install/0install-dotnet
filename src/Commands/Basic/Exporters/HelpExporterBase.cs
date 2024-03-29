// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using System.Text.RegularExpressions;

namespace ZeroInstall.Commands.Basic.Exporters;

/// <summary>
/// Common base class for exporting all <see cref="CliCommand"/> help texts in a structured text format.
/// </summary>
public abstract class HelpExporterBase
{
    /// <summary>
    /// Returns all <see cref="CliCommand"/> help texts in a structured text format.
    /// </summary>
    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine(CommandListHeader());
        ForEachCommand(command => builder.AppendLine(CommandListEntry(command.FullName, command.Description)));
        builder.AppendLine(CommandListFooter());

        ForEachCommand(command =>
        {
            builder.AppendLine(CommandDetails(command.FullName, command.Description, command.Usage));
            builder.AppendLine(OptionListHeader());
            foreach (var option in command.Options.Where(x => x.Prototype != "<>" && x.Description != null))
                builder.AppendLine(OptionListEntry(GetPrototypes(option), option.Description));
            builder.AppendLine(OptionListFooter());
        });

        return builder.ToString();
    }

    private static void ForEachCommand(Action<CliCommand> action)
    {
        using var handler = new CliCommandHandler();
        foreach (string commandName in CliCommand.Names)
        {
            var command = CliCommand.Create(commandName, handler);
            if (command is CliMultiCommand multiCommand)
            {
                foreach (string stringCommandName in multiCommand.SubCommandNames)
                    action(multiCommand.GetCommand(stringCommandName));
            }
            else
                action(command);
        }
    }

    private static readonly Regex _descriptionParameterRegex = new("{([A-Z]*)}");

    private static IEnumerable<string> GetPrototypes(Option option)
    {
        // ReSharper disable once RedundantEnumerableCastCall
        var parameters = _descriptionParameterRegex.Matches(option.Description).Cast<Match>().Select(x => x.Captures[0].Value).ToList();
        var prototypes = option.Prototype.TrimEnd('=').Split('|').Select(x => (x.Length == 1 ? $"-{x}" : $"--{x}"));
        if (parameters.Count > 0) prototypes = prototypes.Select(x => $"{x} {string.Join(" ", parameters)}");
        return prototypes;
    }

    protected abstract string CommandListHeader();
    protected abstract string CommandListEntry(string? name, string description);
    protected abstract string CommandListFooter();
    protected abstract string CommandDetails(string? name, string description, string usage);
    protected abstract string OptionListHeader();
    protected abstract string OptionListEntry(IEnumerable<string> prototypes, string description);
    protected abstract string OptionListFooter();
}
