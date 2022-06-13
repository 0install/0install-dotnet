// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Archives.Extractors;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Calculate the manifest digest of a directory or archive.
/// </summary>
public class Digest : CliCommand
{
    public const string Name = "digest";
    public override string Description => Resources.DescriptionDigest;
    public override string Usage => "(DIRECTORY | ARCHIVE [SUBDIR])";
    protected override int AdditionalArgsMin => 1;
    protected override int AdditionalArgsMax => 2;

    /// <summary>The hashing algorithm used to generate the manifest.</summary>
    private ManifestFormat _algorithm = ManifestFormat.Sha1New;

    private bool _printManifest, _printDigest;

    /// <inheritdoc/>
    public Digest(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("manifest", () => Resources.OptionManifest, _ => _printManifest = true);
        Options.Add("digest", () => Resources.OptionDigest, _ => _printDigest = true);
        Options.Add("algorithm=", () => Resources.OptionAlgorithm + Environment.NewLine + SupportedValues(ManifestFormat.All),
            delegate(string algorithm)
            {
                try
                {
                    _algorithm = ManifestFormat.FromPrefix(algorithm);
                }
                #region Error handling
                catch (NotSupportedException ex)
                {
                    // Wrap exception since only certain exception types are allowed
                    throw new OptionException(ex.Message, algorithm);
                }
                #endregion
            });
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        var manifest = GenerateManifest(
            AdditionalArgs[0],
            (AdditionalArgs.Count == 2) ? AdditionalArgs[1] : null);

        Handler.Output("Manifest digest", GetOutput(manifest));
        return ExitCode.OK;
    }

    private Manifest GenerateManifest(string path, string? subdir)
    {
        var builder = new ManifestBuilder(_algorithm);

        if (Directory.Exists(path))
        {
            if (!string.IsNullOrEmpty(subdir)) throw new OptionException(Resources.TooManyArguments + Environment.NewLine + subdir.EscapeArgument(), null);

            Handler.RunTask(new ReadDirectory(path, builder));
            return builder.Manifest;
        }
        else if (File.Exists(path))
        {
            var extractor = ArchiveExtractor.For(Archive.GuessMimeType(path), Handler);
            Handler.RunTask(new ReadFile(path, stream => extractor.Extract(builder, stream, subdir)));
            return builder.Manifest;
        }
        else throw new FileNotFoundException(string.Format(Resources.FileOrDirNotFound, path));
    }

    private string GetOutput(Manifest manifest)
    {
        if (_printManifest)
        {
            string result = manifest.ToString();
            if (_printDigest) result += Environment.NewLine + manifest.CalculateDigest();
            return result;
        }
        else return manifest.CalculateDigest();
    }
}
