// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Capabilities;
using ZeroInstall.Publish.EntryPoints;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish;

/// <summary>
/// Builds simple <see cref="Feed"/>s containing a single <see cref="Implementation"/>.
/// </summary>
public class FeedBuilder : IDisposable
{
    #region Directories
    private TemporaryDirectory? _temporaryDirectory;

    /// <summary>
    /// A temporary directory to prepare files for <see cref="ImplementationDirectory"/>. Not used by the <see cref="FeedBuilder"/> itself.
    /// </summary>
    /// <remarks>Setting a new value will automatically <see cref="IDisposable.Dispose"/> the previous one.</remarks>
    public TemporaryDirectory? TemporaryDirectory
    {
        get => _temporaryDirectory;
        set
        {
            _temporaryDirectory?.Dispose();
            _temporaryDirectory = value;
        }
    }

    /// <summary>
    /// Deletes the <see cref="TemporaryDirectory"/>.
    /// </summary>
    public void Dispose()
    {
        _temporaryDirectory?.Dispose();
    }

    /// <summary>
    /// Set the directory to search for <see cref="Candidates"/> and to generate the <see cref="ManifestDigest"/> from.
    /// Is usually a subdirectory of or equal to <see cref="TemporaryDirectory"/>.
    /// </summary>
    public string? ImplementationDirectory { get; set; }
    #endregion

    #region Candidates
    private readonly List<Candidate> _candidates = new();

    /// <summary>
    /// Lists auto-detected candidates for <see cref="EntryPoint"/>s.
    /// </summary>
    public IEnumerable<Candidate> Candidates => _candidates;

    /// <summary>
    /// Set the main entry point. Provides meta-data and startup instructions for the application.
    /// Should be one of the auto-detected <see cref="Candidates"/>.
    /// </summary>
    public Candidate? MainCandidate { get; set; }

    /// <summary>
    /// Detects <see cref="Candidates"/> in the <see cref="ImplementationDirectory"/>.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
    /// <exception cref="InvalidOperationException"><see cref="ImplementationDirectory"/> is <c>null</c> or empty.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">There was a problem generating the manifest or detecting the executables.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to temporary files was not permitted.</exception>
    public void DetectCandidates(ITaskHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (string.IsNullOrEmpty(ImplementationDirectory)) throw new InvalidOperationException("Implementation directory is not set.");

        var detect = new DetectCandidates(ImplementationDirectory);
        handler.RunTask(detect);

        _candidates.Clear();
        _candidates.AddRange(detect.Candidates);
        MainCandidate = _candidates.FirstOrDefault();
    }
    #endregion

    #region Commands
    /// <summary>
    /// Lists the <see cref="Command"/> derived from <see cref="Candidates"/> and <see cref="MainCandidate"/>.
    /// </summary>
    public List<Command> Commands { get; } = new();

    /// <summary>
    /// Lists the <see cref="EntryPoint"/>s accompanying <see cref="Commands"/>.
    /// </summary>
    public List<EntryPoint> EntryPoints { get; } = new();

    /// <summary>
    /// Generates <see cref="Commands"/> and <see cref="EntryPoints"/> bases on <see cref="Candidates"/> and <see cref="MainCandidate"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="MainCandidate"/> is <c>null</c>.</exception>
    public void GenerateCommands()
    {
        if (MainCandidate == null) throw new InvalidOperationException(Resources.EntryPointNotFound);

        Commands.Clear();
        EntryPoints.Clear();

        var mainCommand = MainCandidate.CreateCommand();
        mainCommand.Name = Command.NameRun;
        Commands.Add(mainCommand);

        EntryPoints.Add(new EntryPoint
        {
            Command = Command.NameRun,
            BinaryName = Path.GetFileNameWithoutExtension(MainCandidate.RelativePath)
        });

        foreach (var candidate in _candidates.Except(MainCandidate))
        {
            var command = candidate.CreateCommand();
            Commands.Add(command);

            var entryPoint = new EntryPoint
            {
                Command = command.Name,
                BinaryName = Path.GetFileNameWithoutExtension(candidate.RelativePath),
                NeedsTerminal = candidate.NeedsTerminal
            };
            if (!string.IsNullOrEmpty(candidate.Name)) entryPoint.Names.Add(candidate.Name);
            if (!string.IsNullOrEmpty(candidate.Summary)) entryPoint.Summaries.Add(candidate.Summary);
            EntryPoints.Add(entryPoint);
        }
    }
    #endregion

    #region Manifest digest
    /// <summary>
    /// The value used for <see cref="ImplementationBase.ID"/>.
    /// </summary>
    public string? ID { get; private set; }

    /// <summary>
    /// The value used for <see cref="ImplementationBase.ManifestDigest"/>.
    /// </summary>
    public ManifestDigest ManifestDigest { get; private set; }

    /// <summary>
    /// Generates the <see cref="ID"/> and <see cref="ManifestDigest"/>.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
    /// <exception cref="InvalidOperationException"><see cref="ImplementationDirectory"/> is <c>null</c> or empty.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">There was a problem generating the manifest.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to temporary files was not permitted.</exception>
    public void GenerateDigest(ITaskHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (string.IsNullOrEmpty(ImplementationDirectory)) throw new InvalidOperationException("Implementation directory is not set.");

        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        handler.RunTask(new ReadDirectory(ImplementationDirectory, builder));
        ID = builder.Manifest.CalculateDigest();
        ManifestDigest = new ManifestDigest(ID);
    }
    #endregion

    #region Feed
    /// <summary>
    /// Set to configure <see cref="Feed.Uri"/>.
    /// </summary>
    public FeedUri? Uri { get; set; }

    private readonly List<Icon> _icons = new();

    /// <summary>
    /// Set to configure <see cref="Feed.Icons"/>.
    /// </summary>
    public ICollection<Icon> Icons => _icons;

    /// <summary>
    /// Set to configure <see cref="Implementation.RetrievalMethods"/>.
    /// </summary>
    public RetrievalMethod? RetrievalMethod { get; set; }

    /// <summary>
    /// Set to configure <see cref="Feed.CapabilityLists"/>.
    /// </summary>
    public CapabilityList? CapabilityList { get; set; }

    /// <summary>
    /// Set to configure <see cref="SignedFeed.SecretKey"/>.
    /// </summary>
    public OpenPgpSecretKey? SecretKey { get; set; }

    /// <summary>
    /// Generates a feed as described by the properties.
    /// </summary>
    /// <exception cref="InvalidOperationException">Required values have not been generated or set yet.</exception>
    public SignedFeed Build()
    {
        if (MainCandidate == null) throw new InvalidOperationException($"{nameof(MainCandidate)} is not set.");

        var implementation = new Implementation
        {
            ID = ID ?? throw new InvalidOperationException($"{nameof(GenerateDigest)}() has not been called yet."),
            ManifestDigest = ManifestDigest,
            Version = MainCandidate.Version ?? new ImplementationVersion("1"),
            Architecture = MainCandidate.Architecture
        };
        implementation.Commands.AddRange(Commands);
        if (RetrievalMethod != null) implementation.RetrievalMethods.Add(RetrievalMethod);

        var feed = new Feed
        {
            Name = MainCandidate.Name ?? throw new InvalidOperationException("Name is not set."),
            Summaries = {MainCandidate.Summary ?? throw new InvalidOperationException("Summary is not set.")},
            NeedsTerminal = MainCandidate.NeedsTerminal,
            Elements = {implementation}
        };
        if (Uri != null) feed.Uri = Uri;
        feed.Icons.AddRange(_icons);
        if (!string.IsNullOrEmpty(MainCandidate.Category)) feed.Categories.Add(new Category {Name = MainCandidate.Category});
        feed.EntryPoints.AddRange(EntryPoints);
        if (CapabilityList != null && CapabilityList.Entries.Count != 0) feed.CapabilityLists.Add(CapabilityList);

        return new SignedFeed(feed, SecretKey);
    }
    #endregion
}
