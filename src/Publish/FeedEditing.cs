// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Undo;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish;

/// <summary>
/// Represents a <see cref="Feed"/> being edited using <see cref="IUndoCommand"/>s.
/// </summary>
public class FeedEditing : CommandManager<Feed>
{
    /// <summary>
    /// The (optionally signed) feed being edited.
    /// </summary>
    public SignedFeed SignedFeed { get; private set; }

    /// <summary>
    /// The passphrase to use to unlock <see cref="Publish.SignedFeed.SecretKey"/> (if specified).
    /// </summary>
    public string? Passphrase { get; set; }

    /// <summary>
    /// Indicates whether there are changes to the feed that have not yet been saved to a file.
    /// </summary>
    public bool UnsavedChanges => UndoEnabled
                               || (string.IsNullOrEmpty(Path) && Target?.Elements.Count != 0); // Generated programmatically

    /// <summary>
    /// Starts with an existing feed.
    /// </summary>
    /// <param name="signedFeed">The feed to be edited.</param>
    public FeedEditing(SignedFeed signedFeed)
        : base(signedFeed.Feed)
    {
        SignedFeed = signedFeed ?? throw new ArgumentNullException(nameof(signedFeed));

        // Keep Target and SignedFeed in sync even if Target is replaced with a new object
        TargetUpdated += () => SignedFeed = new SignedFeed(Target!, SignedFeed.SecretKey);
    }

    /// <summary>
    /// Starts with an empty feed.
    /// </summary>
    public FeedEditing()
        : this(new SignedFeed(new Feed()))
    {}

    /// <summary>
    /// Determines whether the feed is valid and ready for use by 0install.
    /// </summary>
    /// <param name="problem">Returns human-readable description of the problem if the method result is <c>false</c>.</param>
    /// <returns><c>true</c> if the feed is valid; <c>false</c> otherwise.</returns>
    public bool IsValid([MaybeNullWhen(true)] out string problem)
    {
        try
        {
            SignedFeed.Feed
                      .Clone()
                      .Normalize(string.IsNullOrEmpty(Path) ? null : new(Path));
            problem = null;
            return true;
        }
        catch (InvalidDataException ex)
        {
            problem = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Saves feed to an XML file, adds the default stylesheet and signs it with <see cref="Publish.SignedFeed.SecretKey"/> (if specified).
    /// </summary>
    /// <remarks>Writing and signing the feed file are performed as an atomic operation (i.e. if signing fails an existing file remains unchanged).</remarks>
    /// <param name="path">The file to save to.</param>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    /// <exception cref="KeyNotFoundException">The specified <see cref="Publish.SignedFeed.SecretKey"/> could not be found on the system.</exception>
    /// <exception cref="WrongPassphraseException"><see cref="Passphrase"/> was incorrect.</exception>
    public override void Save(string path)
    {
        SignedFeed.Save(path, Passphrase);

        Path = path;
        ClearUndo();
    }

    /// <summary>
    /// Loads a feed from an XML file (feed).
    /// </summary>
    /// <param name="path">The file to load from.</param>
    /// <returns>A <see cref="FeedEditing"/> containing the loaded feed.</returns>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
    public new static FeedEditing Load(string path)
        => new(SignedFeed.Load(path)) {Path = path};
}
