// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using NanoByte.Common.Undo;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish;

/// <summary>
/// Helpers for setting missing properties on <see cref="RetrievalMethod"/>s.
/// </summary>
public static class RetrievalMethodExtensions
{
    /// <summary>
    /// Determines if a specific digest format should be calculated based on an empty string value.
    /// </summary>
    /// <remarks>
    /// Empty strings are used in 0template to indicate that the user wishes this value to be calculated.
    /// This is the same convention used in <see cref="ImplementationExtensions.IsDigestMissing"/>.
    /// </remarks>
    internal static bool ShouldCalculateFormat(ManifestFormat format, ManifestDigest existingDigest)
        => format.Prefix switch
        {
            "sha1new" => existingDigest.Sha1New == "",
            "sha256" => existingDigest.Sha256 == "",
            "sha256new" => existingDigest.Sha256New == "",
            _ => false
        };

    /// <summary>
    /// Merges a newly calculated digest value into an existing digest structure.
    /// </summary>
    internal static ManifestDigest MergeDigest(ManifestDigest existing, ManifestFormat format, ManifestDigest newDigest)
        => format.Prefix switch
        {
            "sha1new" => existing with { Sha1New = newDigest.Sha1New },
            "sha256" => existing with { Sha256 = newDigest.Sha256 },
            "sha256new" => existing with { Sha256New = newDigest.Sha256New },
            _ => existing
        };
    /// <summary>
    /// Sets missing properties on the retrieval method if they can be inferred.
    /// </summary>
    /// <param name="retrievalMethod">The retrieval method.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="localPath">An optional local file path where the <paramref name="retrievalMethod"/> has already been downloaded.</param>
    public static void SetMissing(this DownloadRetrievalMethod retrievalMethod, ICommandExecutor executor, string? localPath = null)
    {
        #region Sanity checks
        if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        #endregion

        switch (retrievalMethod)
        {
            case Archive archive when string.IsNullOrEmpty(archive.MimeType):
                executor.Execute(SetValueCommand.ForNullable(() => archive.MimeType,
                    Archive.GuessMimeType(localPath ?? archive.Href.OriginalString)));
                break;

            case SingleFile file when string.IsNullOrEmpty(file.Destination):
                executor.Execute(SetValueCommand.For(() => file.Destination,
                    Path.GetFileName(localPath ?? file.Href.GetLocalFileName())));
                break;
        }
    }

    /// <summary>
    /// Calculates a <see cref="ManifestDigest"/> for a retrieval method. Sets missing properties in the process.
    /// </summary>
    /// <param name="retrievalMethod">The retrieval method.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <param name="format">The manifest format. Leave <c>null</c> for default.</param>
    /// <returns>The generated digest.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
    public static ManifestDigest CalculateDigest(this RetrievalMethod retrievalMethod, ICommandExecutor executor, ITaskHandler handler, ManifestFormat? format = null)
    {
        #region Sanity checks
        if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        var builder = new ManifestBuilder(format ?? ManifestFormat.Sha256New);
        builder.Add(retrievalMethod, executor, handler);

        var digest = new ManifestDigest(builder.Manifest.CalculateDigest());
        if (digest.PartialEquals(ManifestDigest.Empty))
            Log.Warn(string.Format(Resources.EmptyImplementation, "extract"));
        return digest;
    }

    /// <summary>
    /// Calculates a <see cref="ManifestDigest"/> with multiple formats for a retrieval method. Sets missing properties in the process.
    /// </summary>
    /// <param name="retrievalMethod">The retrieval method.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <param name="existingDigest">An existing digest indicating which formats to calculate (empty string values indicate formats to calculate).</param>
    /// <returns>The generated digest with all requested formats populated.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
    public static ManifestDigest CalculateDigest(this RetrievalMethod retrievalMethod, ICommandExecutor executor, ITaskHandler handler, ManifestDigest existingDigest)
    {
        #region Sanity checks
        if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        var result = existingDigest;

        // Calculate each requested format (indicated by empty string values)
        foreach (var format in ManifestFormat.All)
        {
            if (ShouldCalculateFormat(format, existingDigest))
            {
                var builder = new ManifestBuilder(format);
                builder.Add(retrievalMethod, executor, handler);
                string digestValue = builder.Manifest.CalculateDigest();
                
                // Parse and extract the hash value from the digest string
                var tempDigest = new ManifestDigest(digestValue);
                
                // Merge the calculated digest into the result
                result = MergeDigest(result, format, tempDigest);
            }
        }

        if (result.PartialEquals(ManifestDigest.Empty))
            Log.Warn(string.Format(Resources.EmptyImplementation, "extract"));
        
        return result;
    }

    /// <summary>
    /// Creates a temporary directory from a retrieval method. Sets missing properties in the process.
    /// </summary>
    /// <param name="retrievalMethod">The retrieval method.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <param name="localPath">An optional local file path where the <paramref name="retrievalMethod"/> has already been downloaded. Leave <c>null</c> to download automatically.</param>
    /// <returns>A temporary directory built using the retrieval method.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
    /// <exception cref="IOException">There is a problem writing a temporary file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a temporary file is not permitted.</exception>
    [MustDisposeResource]
    public static TemporaryDirectory ToTempDir(this DownloadRetrievalMethod retrievalMethod, ITaskHandler handler, string? localPath = null)
    {
        #region Sanity checks
        if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        var tempDir = new TemporaryDirectory("0publish");
        try
        {
            var builder = new DirectoryBuilder(tempDir);
            builder.Add(retrievalMethod, new SimpleCommandExecutor(), handler, localPath);
            return tempDir;
        }
        catch
        {
            tempDir.Dispose();
            throw;
        }
    }
}
