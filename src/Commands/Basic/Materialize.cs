// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Services.Executors;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Copies the program specified by URI and all its dependencies into a directory, along with a launcher script that runs it without Zero Install.
/// </summary>
public class Materialize : Download
{
    public new const string Name = "materialize";
    public override string Description => Resources.DescriptionMaterialize;
    public override string Usage => "[OPTIONS] URI DIRECTORY";
    protected override int AdditionalArgsMin => 2;
    protected override int AdditionalArgsMax => 2;

    /// <summary>Indicates the user wants links to the implementations instead of copies.</summary>
    private bool _link;

    /// <summary>
    /// Creates a new materialize command.
    /// </summary>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    public Materialize(ICommandHandler handler)
        : base(handler, outputOptions: false)
    {
        Options.Add("link", () => Resources.OptionLink, _ => _link = true);
    }

    private string? _outputPath;

    /// <inheritdoc/>
    public override void Parse(IReadOnlyList<string> args)
    {
        base.Parse(args);

        _outputPath = Paths.Absolute(AdditionalArgs[0]);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        Solve();
        DownloadUncachedImplementations();

        string outputPath = _outputPath ?? throw new InvalidOperationException($"Must run {nameof(Parse)}() first.");
        var materializer = new Materializer(ImplementationStore, Handler)
        {
            TargetOS = Requirements.ForCurrentSystem().Architecture.OS,
            UseLinks = _link
        };
        string launcherPath = materializer.Materialize(Selections, outputPath, GetLauncherName(Requirements.InterfaceUri));

        BackgroundSelfUpdate();

        Handler.OutputLow(
            title: Resources.MaterializeComplete,
            message: string.Format(Resources.AllComponentsMaterialized, Selections.Name, outputPath, launcherPath));
        return ExitCode.OK;
    }

    /// <summary>
    /// Derives the file name for the launcher script from the file name in the feed URI, e.g. <c>hello</c> for <c>https://example.com/hello.xml</c>.
    /// </summary>
    internal static string GetLauncherName(FeedUri uri)
    {
        string path = uri.IsFile ? uri.LocalPath : Uri.UnescapeDataString(uri.AbsolutePath);
        string name = Path.GetFileNameWithoutExtension(path.TrimEnd('/', '\\'));
        return string.IsNullOrEmpty(name) ? "run" : name;
    }
}
