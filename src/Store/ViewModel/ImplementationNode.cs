// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about an implementation in an <see cref="IImplementationStore"/> for display in a UI.
/// </summary>
public abstract class ImplementationNode : StoreNode
{
    /// <summary>
    /// Creates a new implementation node.
    /// </summary>
    /// <param name="digest">The digest identifying the implementation.</param>
    /// <param name="implementationStore">The <see cref="IImplementationStore"/> the implementation is located in.</param>
    /// <exception cref="FormatException">The manifest file is not valid.</exception>
    /// <exception cref="IOException">The manifest file could not be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    protected ImplementationNode(ManifestDigest digest, IImplementationStore implementationStore)
        : base(implementationStore)
    {
        #region Sanity checks
        if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
        #endregion

        _digest = digest;

        // Determine the total size of an implementation via its manifest file
        string? path = implementationStore.GetPath(digest);
        if (path == null) return;
        string manifestPath = System.IO.Path.Combine(path, Manifest.ManifestFile);
        Size = Manifest.Load(manifestPath, ManifestFormat.FromPrefix(digest.AvailableDigests.First())).TotalSize;
    }

    private readonly ManifestDigest _digest;

    /// <summary>
    /// The digest identifying the implementation in the store.
    /// </summary>
    [Description("The digest identifying the implementation in the store.")]
    public string Digest => _digest.AvailableDigests.First();

    /// <summary>
    /// The total size of the implementation in bytes.
    /// </summary>
    [Browsable(false)]
    public long Size { get; }

    /// <summary>
    /// The total size of the implementation in human-readable form.
    /// </summary>
    [DisplayName("Size"), Description("The total size of the implementation.")]
    public string SizeHuman => Size.FormatBytes();

    /// <inheritdoc/>
    public override string? Path => ImplementationStore.GetPath(_digest);

    /// <summary>
    /// Deletes this implementation from the <see cref="IImplementationStore"/> it is located in.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
    /// <exception cref="KeyNotFoundException">No matching implementation could be found in the <see cref="IImplementationStore"/>.</exception>
    /// <exception cref="IOException">The implementation could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
    public override void Delete(ITaskHandler handler)
    {
        try
        {
            handler.RunTask(new SimpleTask(
                string.Format(Resources.DeletingImplementation, _digest),
                () => ImplementationStore.Remove(_digest, handler)));
        }
        #region Error handling
        catch (ImplementationNotFoundException ex)
        {
            throw new KeyNotFoundException(ex.Message, ex);
        }
        #endregion
    }

    /// <summary>
    /// Verify this implementation is undamaged.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">The entry's directory could not be processed.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the entry's directory is not permitted.</exception>
    public void Verify(ITaskHandler handler) => ImplementationStore.Verify(_digest, handler);

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => $"{Digest}: {Path}";
}
