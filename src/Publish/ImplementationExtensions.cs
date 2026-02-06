// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

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
        if (manifest.GetTopLevelFiles().Count == 0 && manifest.GetTopLevelDirectories() is [var singleDir])
            Log.Warn(string.Format(Resources.ArchiveContainsSingleTopLevelDirectory, singleDir, "extract"));

        foreach (var command in implementation.Commands)
        {
            if (!string.IsNullOrEmpty(command.Path) && !manifest.ContainsFile(command.Path))
                Log.Warn(string.Format(Resources.CommandPathNotFound, command.Name, command.Path));
        }
    }
}
