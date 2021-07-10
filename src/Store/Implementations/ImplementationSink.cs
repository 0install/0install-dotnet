// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using NanoByte.Common.Threading;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Build;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Accepts implementations and stores them.
    /// </summary>
    public class ImplementationSink : MarshalNoTimeout, IImplementationSink
    {
        /// <summary>Controls whether implementation directories are made write-protected once added to prevent unintentional modification (which would invalidate the manifest digests).</summary>
        protected readonly bool UseWriteProtection;

        /// <summary>Indicates whether <see cref="Path"/> is located on a filesystem with support for Unixoid features such as executable bits.</summary>
        protected readonly bool IsUnixFS;

        /// <summary>Indicates whether this implementation sink does not support write access.</summary>
        protected readonly bool ReadOnly;

        /// <summary>
        /// The path to the underlying directory in the file system.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a new implementation sink using a specific path to a directory.
        /// </summary>
        /// <param name="path">A fully qualified directory path. The directory will be created if it doesn't exist yet.</param>
        /// <param name="useWriteProtection">Controls whether implementation directories are made write-protected once added to prevent unintentional modification (which would invalidate the manifest digests).</param>
        /// <exception cref="IOException">The <paramref name="path"/> could not be created or the underlying filesystem can not store file-changed times accurate to the second.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating the <paramref name="path"/> is not permitted.</exception>
        public ImplementationSink(string path, bool useWriteProtection = true)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            try
            {
                Path = System.IO.Path.GetFullPath(path);
                if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);
            }
            #region Error handling
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion

            UseWriteProtection = useWriteProtection;
            IsUnixFS = FlagUtils.IsUnixFS(Path);

            try
            {
                // Ensure the filesystem can store file-changed times accurate to the second (otherwise ManifestDigests will break)
                if (FileUtils.DetermineTimeAccuracy(Path) > 0)
                    throw new IOException(Resources.InsufficientFSTimeAccuracy);

                if (!IsUnixFS) FlagUtils.MarkAsNoUnixFS(Path);
                if (UseWriteProtection && WindowsUtils.IsWindowsNT)
                {
                    File.WriteAllText(
                        path: System.IO.Path.Combine(Path, Resources.DeleteInfoFileName + ".txt"),
                        contents: string.Format(Resources.DeleteInfoFileContent, Path),
                        encoding: Encoding.UTF8);
                }
            }
            catch (IOException ex)
            {
                Log.Debug(ex);
                ReadOnly = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Debug(ex);
                ReadOnly = true;
            }
        }

        /// <inheritdoc/>
        public void Add(ManifestDigest manifestDigest, ITaskHandler handler, params IImplementationSource[] sources)
        {
            #region Sanity checks
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (manifestDigest.Best == null) throw new NotSupportedException(Resources.NoKnownDigestMethod);
            if (manifestDigest.AvailableDigests.Any(digest => Directory.Exists(System.IO.Path.Combine(Path, digest)))) throw new ImplementationAlreadyInStoreException(manifestDigest);
            Log.Debug($"Storing implementation {manifestDigest.Best} in {this}");

            // Build in a temporary directory inside the sink so it can be validated safely (no manipulation of directory while validating)
            string tempDir = GetTempDir();
            try
            {
                foreach (var source in sources)
                {
                    var task = source.GetApplyTask(tempDir);
                    task.Tag = manifestDigest.Best;
                    try
                    {
                        using (task as IDisposable)
                            handler.RunTask(task);
                    }
                    #region Error handling
                    catch (IOException ex)
                    {
                        if (source is ArchiveImplementationSource archive)
                            throw new IOException(string.Format(Resources.FailedToExtractArchive, archive.OriginalSource), ex);
                    }
                    #endregion
                }

                VerifyAndAdd(System.IO.Path.GetFileName(tempDir), manifestDigest, handler);
            }
            finally
            {
                DeleteTempDir(tempDir);
            }
        }

        private readonly object _renameLock = new();

        /// <summary>
        /// Verifies the <see cref="ManifestDigest"/> of a directory temporarily stored inside the sink and moves it to the final location if it passes.
        /// </summary>
        /// <param name="tempID">The temporary identifier of the directory inside the sink.</param>
        /// <param name="expectedDigest">The digest the <see cref="Implementation"/> is supposed to match.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <exception cref="DigestMismatchException">The temporary directory doesn't match the <paramref name="expectedDigest"/>.</exception>
        /// <exception cref="IOException"><paramref name="tempID"/> cannot be moved or the digest cannot be calculated.</exception>
        /// <exception cref="ImplementationAlreadyInStoreException">There is already an <see cref="Implementation"/> with the specified <paramref name="expectedDigest"/> in the store.</exception>
        private void VerifyAndAdd(string tempID, ManifestDigest expectedDigest, ITaskHandler handler)
        {
            // Determine the digest method to use
            string? expectedDigestValue = expectedDigest.Best;
            if (string.IsNullOrEmpty(expectedDigestValue)) throw new NotSupportedException(Resources.NoKnownDigestMethod);

            // Determine the source and target directories
            string source = System.IO.Path.Combine(Path, tempID);
            string target = System.IO.Path.Combine(Path, expectedDigestValue);

            if (IsUnixFS) FlagUtils.ConvertToFS(source);

            // Calculate the actual digest, compare it with the expected one and create a manifest file
            VerifyDirectory(source, expectedDigest, handler).Save(System.IO.Path.Combine(source, Manifest.ManifestFile));

            lock (_renameLock) // Prevent race-conditions when adding the same digest twice
            {
                if (Directory.Exists(target)) throw new ImplementationAlreadyInStoreException(expectedDigest);

                // Move directory to final destination
                try
                {
                    Directory.Move(source, target);
                }
                catch (IOException ex) when (ex.Message.Contains("already exists") || Directory.Exists(target))
                {
                    throw new ImplementationAlreadyInStoreException(expectedDigest);
                }
            }

            // Prevent any further changes to the directory
            if (UseWriteProtection) EnableWriteProtection(target);
        }

        /// <summary>
        /// Creates a temporary directory within <see cref="Path"/>.
        /// </summary>
        /// <returns>The path to the new temporary directory.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Returns a new value on each call")]
        protected string GetTempDir()
        {
            string path = System.IO.Path.Combine(Path, System.IO.Path.GetRandomFileName());
            Log.Debug("Creating temp directory for extracting: " + path);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Deletes a temporary directory.
        /// </summary>
        /// <param name="path">The path to the temporary directory.</param>
        protected void DeleteTempDir(string path)
        {
            if (Directory.Exists(path))
            {
                Log.Debug("Deleting left-over temp directory: " + path);
                Directory.Delete(path, recursive: true);
            }
        }

        /// <summary>
        /// Makes a directory read-only using platform-specific mechanisms. Logs any errors and continues.
        /// </summary>
        /// <param name="path">The directory to protect.</param>
        public static void EnableWriteProtection(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            try
            {
                Log.Debug("Enabling write protection for: " + path);
                FileUtils.EnableWriteProtection(path);
            }
            #region Error handling
            catch (IOException)
            {
                Log.Warn(string.Format(Resources.UnableToWriteProtect, path));
            }
            catch (UnauthorizedAccessException)
            {
                // Only warn if even an Admin is unable to set ACLs
                if (WindowsUtils.IsAdministrator) Log.Warn(string.Format(Resources.UnableToWriteProtect, path));
                else Log.Info(string.Format(Resources.UnableToWriteProtect, path));
            }
            catch (InvalidOperationException)
            {
                Log.Warn(string.Format(Resources.UnableToWriteProtect, path));
            }
            #endregion
        }

        /// <summary>
        /// Removes write-protection from a directory read-only using platform-specific mechanisms. Logs any errors and continues.
        /// </summary>
        /// <param name="path">The directory to unprotect.</param>
        internal static void DisableWriteProtection(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            try
            {
                Log.Debug("Disabling write protection for: " + path);
                FileUtils.DisableWriteProtection(path);
            }
            #region Error handling
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Log.Error(ex);
            }
            #endregion
        }

        /// <summary>
        /// Verifies the <see cref="ManifestDigest"/> of a directory.
        /// </summary>
        /// <param name="directory">The directory to generate a <see cref="Manifest"/> for.</param>
        /// <param name="expectedDigest">The digest the <see cref="Manifest"/> of the <paramref name="directory"/> should have.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>The generated <see cref="Manifest"/>.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">The <paramref name="directory"/> could not be processed.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the <paramref name="directory"/> is not permitted.</exception>
        /// <exception cref="DigestMismatchException">The <paramref name="directory"/> doesn't match the <paramref name="expectedDigest"/>.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public static Manifest VerifyDirectory(string directory, ManifestDigest expectedDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException(nameof(directory));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            string? expectedDigestValue = expectedDigest.Best;
            if (string.IsNullOrEmpty(expectedDigestValue)) throw new NotSupportedException(Resources.NoKnownDigestMethod);
            var format = ManifestFormat.FromPrefix(expectedDigestValue);

            var generator = new ManifestGenerator(directory, format) {Tag = expectedDigestValue};
            handler.RunTask(generator);
            var manifest = generator.Manifest;
            string digest = manifest.CalculateDigest();

            if (digest != expectedDigestValue)
            {
                var offsetManifest = TryFindOffset(manifest, expectedDigestValue);
                if (offsetManifest != null) return offsetManifest;

                string manifestFilePath = System.IO.Path.Combine(directory, Manifest.ManifestFile);
                var expectedManifest = File.Exists(manifestFilePath) ? Manifest.Load(manifestFilePath, format) : null;
                throw new DigestMismatchException(expectedDigestValue, digest, expectedManifest, manifest);
            }

            return manifest;
        }

        private static Manifest? TryFindOffset(Manifest manifest, string expectedDigest)
        {
            // Walk through all conceivable timezone differences
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
    }
}
