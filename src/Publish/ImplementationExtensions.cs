// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using NanoByte.Common.Undo;
using ZeroInstall.Archives.Builders;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish;

/// <summary>
/// Helpers for setting missing properties on <see cref="Implementation"/>s.
/// </summary>
public static class ImplementationExtensions
{
    /// <summary>
    /// Sets missing properties on the implementation by downloading and inferring.
    /// </summary>
    /// <param name="implementation">The implementation.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
    /// <exception cref="DigestMismatchException">An existing digest does not match the newly calculated one.</exception>
    public static void SetMissing(this Implementation implementation, ICommandExecutor executor, ITaskHandler handler)
    {
        #region Sanity checks
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        try
        {
            implementation.GenerateArchiveIfMissing(executor, handler);
        }
        #region Error handling
        catch (UriFormatException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new WebException(ex.Message, ex);
        }
        #endregion

        foreach (var retrievalMethod in implementation.RetrievalMethods)
        {
            if (IsDigestMissing(implementation) || IsDownloadSizeMissing(retrievalMethod))
                implementation.SetDigest(builder => builder.Add(retrievalMethod, executor, handler), executor);
        }
    }

    /// <summary>
    /// Adds an <see cref="Archive"/> to the implementation and sets its <see cref="ImplementationBase.ManifestDigest"/> and <see cref="ImplementationBase.ID"/> accordingly.
    /// </summary>
    /// <param name="implementation">The implementation.</param>
    /// <param name="href">The URL the archive can be downloaded from.</param>
    /// <param name="localPath">A local copy of the archive; <c>null</c> to look for a file named like <paramref name="href"/> in the current directory.</param>
    /// <param name="extract">The subdirectory of the archive to extract; <c>null</c> for entire archive.</param>
    /// <param name="formats">The manifest formats to calculate digests for. Leave empty for default.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="FileNotFoundException">No local copy of the archive could be found.</exception>
    /// <exception cref="IOException">A problem occurred while extracting the archive.</exception>
    /// <exception cref="NotSupportedException">The archive type could not be determined.</exception>
    /// <exception cref="DigestMismatchException">An existing digest does not match the newly calculated one.</exception>
    public static void AddArchive(this Implementation implementation, Uri href, string? localPath, string? extract, IEnumerable<ManifestFormat>? formats, ICommandExecutor executor, ITaskHandler handler)
    {
        #region Sanity checks
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));
        #endregion

        var (archive, digest) = BuildArchive(href, localPath, extract, formats, executor, handler);
        implementation.AddArchive(archive, digest, executor);
    }

    /// <summary>
    /// Downloads or reads an archive, extracts it and calculates its <see cref="ManifestDigest"/>. Sets missing properties on the archive in the process.
    /// </summary>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="FileNotFoundException">No local copy of the archive could be found.</exception>
    /// <exception cref="IOException">A problem occurred while extracting the archive.</exception>
    /// <exception cref="NotSupportedException">The archive type could not be determined.</exception>
    internal static (Archive Archive, ManifestDigest Digest) BuildArchive(Uri href, string? localPath, string? extract, IEnumerable<ManifestFormat>? formats, ICommandExecutor executor, ITaskHandler handler)
    {
        #region Sanity checks
        if (href == null) throw new ArgumentNullException(nameof(href));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        if (string.IsNullOrEmpty(localPath))
        {
            localPath = Path.Combine(Environment.CurrentDirectory, href.GetLocalFileName());
            if (!File.Exists(localPath))
                throw new FileNotFoundException(string.Format(Resources.ArchiveFileNotFound, localPath, "--archive-file"), localPath);
        }

        var archive = new Archive {Href = href, Extract = extract};
        archive.SetMissing(executor, localPath);

        using var tempDir = archive.ToTempDir(handler, localPath);
        return (archive, BuildDigest(tempDir, formats, handler));
    }

    /// <summary>
    /// Adds an already extracted <paramref name="archive"/> to the implementation and sets its <see cref="ImplementationBase.ManifestDigest"/> and <see cref="ImplementationBase.ID"/> accordingly.
    /// </summary>
    /// <exception cref="DigestMismatchException">An existing digest does not match <paramref name="digest"/>.</exception>
    internal static void AddArchive(this Implementation implementation, Archive archive, ManifestDigest digest, ICommandExecutor executor)
    {
        if (implementation.ManifestDigest == default)
            executor.Execute(SetValueCommand.For(() => implementation.ManifestDigest, newValue: digest));
        else if (!digest.PartialEquals(implementation.ManifestDigest))
            throw new DigestMismatchException(expectedDigest: implementation.ManifestDigest.ToString(), actualDigest: digest.ToString());

        if (digest.Best is {} id && implementation.ID != id)
            executor.Execute(SetValueCommand.For(() => implementation.ID, newValue: id));

        executor.Execute(AddToCollection.For(implementation.RetrievalMethods, archive));
    }

    /// <summary>
    /// Adds a digest in an additional <see cref="ManifestFormat"/> to the implementation, using a cached copy of the implementation.
    /// </summary>
    /// <param name="implementation">The implementation.</param>
    /// <param name="format">The manifest format to add a digest for.</param>
    /// <param name="implementationStore">Used to look for a cached copy of the implementation.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <returns><c>true</c> if a digest was added; <c>false</c> if the digest was already present or no cached copy was found.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while reading the cached implementation.</exception>
    /// <exception cref="DigestMismatchException">The cached implementation does not match its expected digest.</exception>
    public static bool AddDigest(this Implementation implementation, ManifestFormat format, IImplementationStore implementationStore, ICommandExecutor executor, ITaskHandler handler)
    {
        #region Sanity checks
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));
        if (format == null) throw new ArgumentNullException(nameof(format));
        if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        // Local implementations have no digest to extend
        if (!string.IsNullOrEmpty(implementation.LocalPath)) return false;

        var oldDigest = implementation.ManifestDigest;
        if (oldDigest.AvailableDigests.Any(x => x.StartsWith(format.Prefix + format.Separator))) return false;

        if (implementationStore.GetPath(oldDigest) is not {} path)
        {
            Log.Info(string.Format(Resources.NoCachedImplementation, implementation.Version));
            return false;
        }
        implementationStore.Verify(oldDigest);

        var newDigest = oldDigest;
        newDigest.TryParse(CalculateDigest(path, format, handler));
        executor.Execute(SetValueCommand.For(() => implementation.ManifestDigest, newValue: newDigest));
        return true;
    }

    /// <summary>
    /// Calculates a <see cref="ManifestDigest"/> for a directory in one or more <see cref="ManifestFormat"/>s.
    /// </summary>
    private static ManifestDigest BuildDigest(string path, IEnumerable<ManifestFormat>? formats, ITaskHandler handler)
    {
        var digest = new ManifestDigest();
        foreach (var format in formats?.ToList() is {Count: > 0} list ? list : [ManifestFormat.Sha256New])
            digest.TryParse(CalculateDigest(path, format, handler));
        return digest;
    }

    private static string CalculateDigest(string path, ManifestFormat format, ITaskHandler handler)
    {
        var builder = new ManifestBuilder(format);
        handler.RunTask(new ReadDirectory(path, builder));
        return builder.Manifest.CalculateDigest();
    }

    private static bool IsDigestMissing(Implementation implementation)
        => implementation.ManifestDigest == default ||
           // Empty strings are used in 0template to indicate that the user wishes this value to be calculated
           implementation.ManifestDigest.Sha1New == "" ||
           implementation.ManifestDigest.Sha256 == "" ||
           implementation.ManifestDigest.Sha256New == "";

    private static bool IsDownloadSizeMissing(RetrievalMethod retrievalMethod)
        => retrievalMethod is DownloadRetrievalMethod {Size: 0};

    private static void GenerateArchiveIfMissing(this Implementation implementation, ICommandExecutor executor, ITaskHandler handler)
    {
        if (string.IsNullOrEmpty(implementation.LocalPath)) return;

        if (implementation
           .RetrievalMethods
           .OfType<Archive>()
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
           .FirstOrDefault(x => string.IsNullOrEmpty(x.Destination) && string.IsNullOrEmpty(x.Extract) && x.Href != null)
            is not {} archive) return;

        string directoryPath = ModelUtils.GetAbsolutePath(implementation.LocalPath, executor.Path);

        var archiveHref = ModelUtils.GetAbsoluteHref(archive.Href, executor.Path);
        if (!archiveHref.IsFile) return;

        implementation.SetDigest(builder => handler.RunTask(new ReadDirectory(directoryPath, builder)), executor);

        archive.SetMissing(executor, archiveHref.LocalPath);
        ArchiveBuilder.RunForDirectory(directoryPath, archiveHref.LocalPath, archive.MimeType!, handler);

        executor.Execute(SetValueCommand.For(() => archive.Size, newValue: new FileInfo(archiveHref.LocalPath).Length));
        // ReSharper disable once RedundantTypeArgumentsOfMethod
        executor.Execute(SetValueCommand.ForNullable(() => implementation.LocalPath, newValue: null));
    }

    private static void SetDigest(this Implementation implementation, Action<IBuilder> build, ICommandExecutor executor)
    {
        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        build(builder);
        var digest = new ManifestDigest(builder.Manifest.CalculateDigest());

        if (IsDigestMissing(implementation))
            executor.Execute(SetValueCommand.For(() => implementation.ManifestDigest, newValue: digest));
        else if (!digest.PartialEquals(implementation.ManifestDigest))
            throw new DigestMismatchException(expectedDigest: implementation.ManifestDigest.ToString(), actualDigest: digest.ToString());

        if (string.IsNullOrEmpty(implementation.ID) && !string.IsNullOrEmpty(digest.Best))
            executor.Execute(SetValueCommand.For(() => implementation.ID, newValue: digest.Best));

        DetectIssues(implementation, builder.Manifest);
    }

    private static void DetectIssues(Implementation implementation, Manifest manifest)
    {
        if (manifest.GetTopLevelFiles().Count == 0 && manifest.GetTopLevelDirectories() is [var singleDir] && !singleDir.EndsWith(".app"))
            Log.Warn(string.Format(Resources.ArchiveContainsSingleTopLevelDirectory, singleDir, "extract"));

        foreach (var command in implementation.Commands)
        {
            if (!string.IsNullOrEmpty(command.Path) && manifest.TryGetElement(command.Path) == null)
                Log.Warn(string.Format(Resources.CommandPathNotFound, command.Name, command.Path));
        }
    }
}
