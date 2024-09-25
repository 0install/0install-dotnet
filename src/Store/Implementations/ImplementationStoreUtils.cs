// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Helper methods for <see cref="IImplementationStore"/>s and paths.
/// </summary>
public static class ImplementationStoreUtils
{
    /// <summary>
    /// Determines the local path of an implementation.
    /// </summary>
    /// <param name="store">The store containing the implementation.</param>
    /// <param name="implementation">The implementation to be located.</param>
    /// <returns>A fully qualified path to the directory containing the implementation.</returns>
    /// <exception cref="ImplementationNotFoundException">The <paramref name="implementation"/> is not cached yet.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the store is not permitted.</exception>
    public static string GetPath(this IImplementationStore store, ImplementationBase implementation)
    {
        #region Sanity checks
        if (store == null) throw new ArgumentNullException(nameof(store));
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));
        #endregion

        if (!string.IsNullOrEmpty(implementation.LocalPath)) return implementation.LocalPath;

        return store.GetPath(implementation.ManifestDigest) ?? throw new ImplementationNotFoundException(implementation.ManifestDigest);
    }

    /// <summary>
    /// Determines whether a path looks like it is inside a store implementation known by <see cref="ManifestFormat"/>.
    /// </summary>
    /// <param name="path">A path to a directory that may or may not be inside a store implementation.</param>
    /// <param name="implementationPath">The top-level of the detected store implementation directory if any; <c>null</c> otherwise.</param>
    /// <remarks>Performs no file system access. Only looks at the path string itself.</remarks>
    public static bool IsImplementation(string path, [NotNullWhen(true)] out string? implementationPath)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        var builder = new StringBuilder();
        foreach (string part in Path.GetFullPath(path).Split(Path.DirectorySeparatorChar))
        {
            builder.Append(part);
            if (ManifestFormat.All.Any(format => part.StartsWith(format.Prefix + format.Separator)))
            {
                implementationPath = builder.ToString();
                return true;
            }
            builder.Append(Path.DirectorySeparatorChar);
        }

        implementationPath = null;
        return false;
    }

    /// <summary>
    /// Checks whether an implementation directory matches the expected digest.
    /// Throws <see cref="DigestMismatchException"/> if it does not match.
    /// </summary>
    /// <param name="path">The path of the directory ot check.</param>
    /// <param name="manifestDigest">The expected digest.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="NotSupportedException"><paramref name="manifestDigest"/> does not list any supported digests.</exception>
    /// <exception cref="IOException">The directory could not be processed.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the directory is not permitted.</exception>
    /// <exception cref="DigestMismatchException">The directory does not match the expected digest</exception>
    public static void Verify(string path, ManifestDigest manifestDigest, ITaskHandler handler)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        string expectedDigest = manifestDigest.Best ?? throw new NotSupportedException(Resources.NoKnownDigestMethod);
        var format = ManifestFormat.FromPrefix(expectedDigest);

        var builder = new ManifestBuilder(format);
        handler.RunTask(new ReadDirectory(path, builder));
        if (Verify(builder.Manifest, expectedDigest) == null)
        {
            throw new DigestMismatchException(
                expectedDigest,
                actualDigest: builder.Manifest.CalculateDigest(),
                expectedManifest: Manifest.TryLoad(Path.Combine(path, Manifest.ManifestFile), format),
                actualManifest: builder.Manifest);
        }
    }

    /// <summary>
    /// Checks whether a <see cref="Manifest"/> matches the expected digest.
    /// Returns the original manifest or one with backwards-compatability modifications applied if it matches.
    /// </summary>
    /// <param name="manifest">The manifest to check.</param>
    /// <param name="expectedDigest">The expected digest.</param>
    /// <returns>The <see cref="Manifest"/> if it matches; <c>null</c> otherwise.</returns>
    internal static Manifest? Verify(Manifest manifest, string expectedDigest)
    {
        if (manifest.CalculateDigest() == expectedDigest) return manifest;

        for (var offset = TimeSpan.FromHours(-27); offset <= TimeSpan.FromHours(27); offset += TimeSpan.FromMinutes(15))
        {
            Log.Debug($"Attempting to correct digest mismatch by shifting timestamps by {offset} and rounding.");
            var offsetManifest = manifest.WithOffset(offset);
            if (offsetManifest.CalculateDigest() == expectedDigest)
            {
                Log.Info($"Expected digest {expectedDigest} but got mismatch. Fixed by shifting timestamps by {offset} and rounding.");
                return offsetManifest;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether all implementations in the store still matches the expected digest.
    /// Asks the user whether to delete the implementation if it does not match.
    /// </summary>
    /// <param name="store">The store containing the implementation.</param>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about IO tasks.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">An implementation's directory could not be processed.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to an implementation's directory is not permitted.</exception>
    public static void Audit(this IImplementationStore store, ITaskHandler handler)
        => handler.RunTask(ForEachTask.Create(Resources.CheckingForDamagedFiles, store.ListAll().ToList(), store.Verify));
}
