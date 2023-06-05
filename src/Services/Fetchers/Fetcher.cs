// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using NanoByte.Common.Threading;
using ZeroInstall.Archives;
using ZeroInstall.Archives.Extractors;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Fetchers;

/// <summary>
/// Downloads <see cref="Implementation"/>s, extracts them and adds them to an <see cref="IImplementationStore"/>.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public partial class Fetcher : IFetcher
{
    /// <summary>User settings controlling network behaviour, solving, etc.</summary>
    protected readonly Config Config;

    /// <summary>The location to store the downloaded and unpacked <see cref="Implementation"/>s in.</summary>
    protected readonly IImplementationStore Store;

    /// <summary>A callback object used when the the user needs to be informed about progress.</summary>
    protected readonly ITaskHandler Handler;

    /// <inheritdoc/>
    public IImplementationDiscovery? Discovery { get; set; }

    /// <inheritdoc/>
    public void Fetch(Implementation implementation)
        => Fetch(
            implementation ?? throw new ArgumentNullException(nameof(implementation)),
            implementation.ManifestDigest.Best ?? implementation.ID);

    /// <summary>
    /// Downloads an <see cref="Implementation"/> to the <see cref="IImplementationStore"/>.
    /// </summary>
    /// <param name="implementation">The implementation to download.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars.</param>
    protected virtual void Fetch(Implementation implementation, string tag)
    {
        // Use mutex to detect in-progress download of same implementation in other processes
        string mutexName = $"0install-fetcher-{implementation.ManifestDigest.Best}";
        using var mutex = new Mutex(false, mutexName);
        try
        {
            while (!mutex.WaitOne(100, exitContext: false)) // NOTE: Might be blocked more than once
                Handler.RunTask(new WaitTask(Resources.WaitingForDownload, mutex) {Tag = tag});
        }
        #region Error handling
        catch (AbandonedMutexException)
        {
            Log.Warn($"Mutex '{mutexName}' was abandoned by another instance");
            // Abandoned mutexes get acquired despite exception
        }
        #endregion

        try
        {
            // Check if another process added the implementation in the meantime
            if (GetPath(implementation) != null) return;

            var retrievalMethods = implementation.RetrievalMethods;
            if (retrievalMethods.Count == 0) throw new NotSupportedException(string.Format(Resources.NoRetrievalMethod, implementation.ID));
            if (retrievalMethods.OfType<ExternalRetrievalMethod>().FirstOrDefault() is {} external)
            {
                Retrieve(external);
                return;
            }

            var manifestDigest = implementation.ManifestDigest;
            if (manifestDigest.Best == null) throw new NotSupportedException(string.Format(Resources.NoManifestDigest, implementation.ID));
            if (DiscoverImplementation(manifestDigest, tag) is {} retrievalMethod)
                retrievalMethods = new(retrievalMethods) {retrievalMethod};
            TryRetrieve(retrievalMethods, manifestDigest, tag);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private RetrievalMethod? DiscoverImplementation(ManifestDigest manifestDigest, string tag)
    {
        if (Discovery == null) return null;

        var task = ResultTask.Create(Resources.DiscoveringImplementation, () => Discovery.TryGetImplementation(manifestDigest, TimeSpan.FromSeconds(2), Handler.CancellationToken));
        task.Tag = tag;
        return Handler.RunTaskAndReturn(task) is {} uri
            ? new Archive { Href = uri, Size = -1 }
            : null;
    }

    /// <summary>
    /// Tries one or more <see cref="RetrievalMethod"/>s until one succeeds.
    /// </summary>
    /// <param name="retrievalMethods">The available retrieval method.</param>
    /// <param name="manifestDigest">The expected manifest digest of the implementation.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars.</param>
    protected virtual void TryRetrieve(IEnumerable<RetrievalMethod> retrievalMethods, ManifestDigest manifestDigest, string tag)
    {
        retrievalMethods
           .OrderBy(x => x, RetrievalMethodRanker.Instance)
           .TryAny(retrievalMethod =>
            {
                Handler.CancellationToken.ThrowIfCancellationRequested();
                try { Retrieve(retrievalMethod, manifestDigest, tag); }
                catch (ImplementationAlreadyInStoreException) {}
            });
    }

    /// <summary>
    /// Executes a retrieval method to build an implementation.
    /// </summary>
    /// <param name="retrievalMethod">The retrieval method.</param>
    /// <param name="manifestDigest">The expected manifest digest of the implementation.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars.</param>
    protected virtual void Retrieve(RetrievalMethod retrievalMethod, ManifestDigest manifestDigest, string tag)
    {
        switch (retrievalMethod)
        {
            case DownloadRetrievalMethod download:
                Retrieve(new[] {download}, manifestDigest, tag);
                break;
            case Recipe recipe:
                Retrieve(recipe.Steps, manifestDigest, tag);
                break;
            default:
                throw new NotSupportedException($"Unknown retrieval method: ${retrievalMethod}");
        }
    }

    /// <summary>
    /// Executes the steps of a retrieval method to build an implementation.
    /// </summary>
    /// <param name="steps">The retrieval method steps.</param>
    /// <param name="manifestDigest">The expected manifest digest of the implementation.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars.</param>
    protected virtual void Retrieve(IReadOnlyCollection<IRecipeStep> steps, ManifestDigest manifestDigest, string tag)
    {
        CheckArchiveTypes(steps.OfType<Archive>());

        try
        {
            Store.Add(manifestDigest, builder =>
            {
                foreach (var step in steps)
                    Apply(builder, step, tag);
            });
        }
        #region Error handling
        catch (DigestMismatchException)
        {
            Log.Error(string.Format(Resources.FetcherProblem, string.Join(", ", steps.Select(x => x.ToString()).WhereNotNull())));
            throw;
        }
        #endregion
    }

    /// <summary>
    /// Infers missing <see cref="Archive.MimeType"/>s and ensures suitable <see cref="IArchiveExtractor"/>s are available.
    /// </summary>
    /// <exception cref="NotSupportedException">No extractor registered for the specified or inferred <see cref="Archive.MimeType"/>.</exception>
    protected void CheckArchiveTypes(IEnumerable<Archive> archives)
    {
        foreach (var archive in archives)
        {
            try
            {
                archive.MimeType ??= Archive.GuessMimeType(archive.Href.OriginalString);
                ArchiveExtractor.For(archive.MimeType, Handler);
            }
            #region Error handling
            catch (NotSupportedException ex)
            {
                // Wrap exception to add context information
                throw new NotSupportedException(string.Format(Resources.FetcherProblem, archive), ex);
            }
            #endregion
        }
    }

    /// <summary>
    /// Applies a retrieval method step.
    /// </summary>
    /// <param name="builder">The builder used to build the implementation.</param>
    /// <param name="step">Instructions for downloading the file.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars.</param>
    protected virtual void Apply(IBuilder builder, IRecipeStep step, string tag)
    {
        switch (step)
        {
            case DownloadRetrievalMethod download:
                Download(builder, download, tag);
                break;
            case RemoveStep remove:
                builder.Remove(remove);
                break;
            case RenameStep rename:
                builder.Rename(rename);
                break;
            case CopyFromStep copyFrom:
                if (copyFrom.Implementation == null) throw new ArgumentException($"Must call {nameof(IRecipeStep.Normalize)}() first.", nameof(step));
                Fetch(copyFrom.Implementation, tag);
                builder.CopyFrom(copyFrom, GetPath(copyFrom.Implementation) ?? throw new IOException($"Unable to resolve {copyFrom.ID}."), Handler);
                break;
            default:
                throw new NotSupportedException($"Unknown recipe step: ${step}");
        }
    }

    /// <summary>
    /// Applies a download step.
    /// </summary>
    /// <param name="builder">The builder used to build the implementation.</param>
    /// <param name="download">Instructions for downloading the file.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars.</param>
    protected virtual void Download(IBuilder builder, DownloadRetrievalMethod download, string tag)
    {
        void Callback(Stream stream) => builder.Add(download, stream, Handler, tag);
        void DownloadFile(Uri uri) => Handler.RunTask(new DownloadFile(uri, Callback, download.DownloadSize) {Tag = tag});

        var uri = download.Href;
        if (uri.IsFile)
        {
            Handler.RunTask(new ReadFile(uri.LocalPath, Callback) {Tag = tag});
            return;
        }

        try
        {
            DownloadFile(uri);
        }
        catch (WebException ex) when (Config.FeedMirror is {} mirror && ex.ShouldTryMirror(uri))
        {
            Log.Warn(string.Format(Resources.TryingFeedMirror, uri), ex);
            try
            {
                DownloadFile(GetMirrorUri(mirror, uri));
            }
            catch (WebException exMirror)
            {
                Log.Debug($"Failed to download archive {uri} from feed mirror.", exMirror);
                throw ex.Rethrow(); // Report the original problem instead of mirror errors
            }
        }
    }

    private static Uri GetMirrorUri(Uri mirrorRoot, Uri originalUri)
        => new($"{mirrorRoot.EnsureTrailingSlash().AbsoluteUri}archive/{originalUri.Scheme}/{originalUri.Host}/{string.Concat(originalUri.Segments).TrimStart('/').Replace("/", "%23")}");

    /// <summary>
    /// Determines the local path of an implementation.
    /// </summary>
    /// <returns>A fully qualified path to the directory containing the implementation; <c>null</c> if the requested implementation could not be found in the store or is a package implementation.</returns>
    protected string? GetPath(ImplementationBase implementation)
        => string.IsNullOrEmpty(implementation.ID) || implementation.ID.StartsWith(ExternalImplementation.PackagePrefix)
            ? null
            : Store.GetPath(implementation.ManifestDigest);

    /// <summary>
    /// Executes an external retrieval method.
    /// </summary>
    protected virtual void Retrieve(ExternalRetrievalMethod retrievalMethod)
    {
        if (retrievalMethod.Install == null) throw new NotSupportedException("No installation callback registered for native package.");

        if (!string.IsNullOrEmpty(retrievalMethod.ConfirmationQuestion))
        {
            if (!Handler.Ask(retrievalMethod.ConfirmationQuestion,
                    defaultAnswer: false, alternateMessage: retrievalMethod.ConfirmationQuestion)) throw new OperationCanceledException();
        }

        retrievalMethod.Install();
    }
}
