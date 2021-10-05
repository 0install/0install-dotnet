// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Threading;
using ZeroInstall.Model;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;
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

            try
            {
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

            if (!ReadOnly)
            {
                if (FileUtils.DetermineTimeAccuracy(Path) > 0)
                    throw new IOException(Resources.InsufficientFSTimeAccuracy);
            }
        }

        /// <inheritdoc/>
        public void Add(ManifestDigest manifestDigest, Action<IBuilder> build)
        {
            #region Sanity checks
            if (build == null) throw new ArgumentNullException(nameof(build));
            #endregion

            if (manifestDigest.AvailableDigests.Any(digest => Directory.Exists(System.IO.Path.Combine(Path, digest)))) throw new ImplementationAlreadyInStoreException(manifestDigest);
            string expectedDigest = manifestDigest.Best ?? throw new NotSupportedException(Resources.NoKnownDigestMethod);
            Log.Debug($"Storing implementation {expectedDigest} in {this}");
            var format = ManifestFormat.FromPrefix(manifestDigest.Best);

            // Place files in temp directory until digest is verified
            string tempDir = GetTempDir();
            using var _ = new Disposable(() => DeleteTempDir(tempDir));

            var builder = new ManifestBuilder(format);
            build(new DirectoryBuilder(tempDir, builder));
            var manifest = ImplementationStoreUtils.Verify(builder.Manifest, expectedDigest);

            if (manifest == null)
            {
                throw new DigestMismatchException(
                    expectedDigest, actualDigest: builder.Manifest.CalculateDigest(),
                    actualManifest: builder.Manifest);
            }
            manifest.Save(System.IO.Path.Combine(tempDir, Manifest.ManifestFile));

            string target = System.IO.Path.Combine(Path, expectedDigest);
            lock (_renameLock) // Prevent race-conditions when adding the same digest twice
            {
                if (Directory.Exists(target)) throw new ImplementationAlreadyInStoreException(manifestDigest);

                // Move directory to final destination
                try
                {
                    Directory.Move(tempDir, target);
                }
                catch (IOException ex) when (ex.Message.Contains("already exists") || Directory.Exists(target))
                {
                    throw new ImplementationAlreadyInStoreException(manifestDigest);
                }
            }

            // Prevent any further changes to the directory
            if (UseWriteProtection) EnableWriteProtection(target);
        }

        private readonly object _renameLock = new();

        private string GetTempDir()
        {
            string path = System.IO.Path.Combine(Path, System.IO.Path.GetRandomFileName());
            Log.Debug("Creating temp directory for extracting: " + path);
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteTempDir(string path)
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
        private static void EnableWriteProtection(string path)
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
    }
}
