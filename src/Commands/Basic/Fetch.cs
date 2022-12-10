// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Downloads a set of <see cref="Implementation"/>s piped in as XML via stdin (for programmatic use). Use <see cref="Feed"/> format with no inner linebreaks and terminated by a single linebreak.
/// </summary>
public class Fetch : CliCommand
{
    public const string Name = "fetch";
    public override string Description => "Downloads a set of implementations piped in as XML via stdin (for programmatic use). Use Feed format with no inner linebreaks and terminated by a single linebreak.";
    public override string Usage => "";
    protected override int AdditionalArgsMax => 0;

    /// <inheritdoc/>
    public Fetch(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input)) return ExitCode.InvalidData;
        Log.Debug($"Fetch input:\n{input}");

        var feedFragment = XmlStorage.FromXmlString<Feed>(input);
        feedFragment.Name = "dummy";
        feedFragment.Normalize();
        FetchAll(feedFragment.Implementations);

        return ExitCode.OK;
    }
}
