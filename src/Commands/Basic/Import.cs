// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Storage;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Import a feed from a local file, as if it had been downloaded from the network.
/// </summary>
/// <remarks>This is useful when testing a feed file, to avoid uploading it to a remote server in order to download it again. The file must have a trusted digital signature, as when fetching from the network.</remarks>
public class Import : CliCommand
{
    public const string Name = "import";
    public override string Description => Resources.DescriptionImport;
    public override string Usage => "FEED-FILE [...]";
    protected override int AdditionalArgsMin => 1;

    /// <inheritdoc/>
    public Import(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        foreach (var file in Paths.ResolveFiles(AdditionalArgs, "*.xml"))
            FeedManager.ImportFeed(file.FullName);

        return ExitCode.OK;
    }
}
