// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Implementations.Build;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Manages a cache directory that stores <see cref="Implementation"/>s, each in its own sub-directory named by its <see cref="ManifestDigest"/>.
    /// </summary>
    /// <remarks>The represented store data is mutable but the class itself is immutable.</remarks>
    public partial class DiskImplementationStore : MarshalNoTimeout, IImplementationStore, IEquatable<DiskImplementationStore>
    {
        /// <inheritdoc/>
        public ImplementationStoreKind Kind { get; }

        /// <inheritdoc/>
        public string DirectoryPath { get; }

        /// <summary>Controls whether implementation directories are made write-protected once added to the cache to prevent unintentional modification (which would invalidate the manifest digests).</summary>
        private readonly bool _useWriteProtection;

        /// <summary>Indicates whether <see cref="DirectoryPath"/> is located on a filesystem with support for Unixoid features such as executable bits.</summary>
        private readonly bool _isUnixFS;

        #region Constructor
        /// <summary>
        /// Creates a new store using a specific path to a cache directory.
        /// </summary>
        /// <param name="directoryPath">A fully qualified directory path. The directory will be created if it doesn't exist yet.</param>
        /// <param name="useWriteProtection">Controls whether implementation directories are made write-protected once added to the cache to prevent unintentional modification (which would invalidate the manifest digests).</param>
        /// <exception cref="IOException">The <paramref name="directoryPath"/> could not be created or the underlying filesystem can not store file-changed times accurate to the second.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating the <paramref name="directoryPath"/> is not permitted.</exception>
        public DiskImplementationStore(string directoryPath, bool useWriteProtection = true)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));
            #endregion

            try
            {
                DirectoryPath = Path.GetFullPath(directoryPath);
                if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);
            }
            #region Error handling
            catch (ArgumentException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            catch (NotSupportedException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion

            Kind = DetermineKind(DirectoryPath);
            _useWriteProtection = useWriteProtection;
            _isUnixFS = FlagUtils.IsUnixFS(DirectoryPath);

            if (Kind == ImplementationStoreKind.ReadWrite)
            {
                try
                {
                    if (!_isUnixFS) FlagUtils.MarkAsNoUnixFS(DirectoryPath);
                    if (_useWriteProtection && WindowsUtils.IsWindowsNT) WriteDeleteInfoFile(DirectoryPath);
                }
                #region Error handling
                catch (IOException ex)
                {
                    // Writing these files is not critical
                    Log.Debug(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Writing these files is not critical
                    Log.Debug(ex);
                }
                #endregion
            }
        }

        private static ImplementationStoreKind DetermineKind(string path)
        {
            try
            {
                // Ensure the store is backed by a filesystem that can store file-changed times accurate to the second (otherwise ManifestDigests will break)
                if (FileUtils.DetermineTimeAccuracy(path) > 0)
                    throw new IOException(Resources.InsufficientFSTimeAccuracy);

                // As a side-effect the test above told us we have write access
                return ImplementationStoreKind.ReadWrite;
            }
            catch (UnauthorizedAccessException)
            {
                // The test could not be performed because we have no write access, which is fine
                return ImplementationStoreKind.ReadOnly;
            }
        }

        private static void WriteDeleteInfoFile(string path)
            => File.WriteAllText(
                path: Path.Combine(path, Resources.DeleteInfoFileName + ".txt"),
                contents: string.Format(Resources.DeleteInfoFileContent, path),
                encoding: Encoding.UTF8);
        #endregion

        //--------------------//

        #region Temp dir
        /// <summary>
        /// Creates a temporary directory within <see cref="DirectoryPath"/>.
        /// </summary>
        /// <returns>The path to the new temporary directory.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Returns a new value on each call")]
        protected virtual string GetTempDir()
        {
            string path = Path.Combine(DirectoryPath, Path.GetRandomFileName());
            Log.Debug("Creating temp directory for extracting: " + path);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Deletes a temporary directory.
        /// </summary>
        /// <param name="path">The path to the temporary directory.</param>
        protected virtual void DeleteTempDir(string path)
        {
            if (Directory.Exists(path))
            {
                Log.Debug("Deleting left-over temp directory: " + path);
                Directory.Delete(path, recursive: true);
            }
        }
        #endregion

        #region Write protection
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
        public static void DisableWriteProtection(string path)
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
            catch (IOException ex)
            {
                Log.Error(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex);
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex);
            }
            #endregion
        }
        #endregion

        #region Verify and add
        private readonly object _renameLock = new object();

        /// <summary>
        /// Verifies the <see cref="ManifestDigest"/> of a directory temporarily stored inside the cache and moves it to the final location if it passes.
        /// </summary>
        /// <param name="tempID">The temporary identifier of the directory inside the cache.</param>
        /// <param name="expectedDigest">The digest the <see cref="Implementation"/> is supposed to match.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>The final location of the directory.</returns>
        /// <exception cref="DigestMismatchException">The temporary directory doesn't match the <paramref name="expectedDigest"/>.</exception>
        /// <exception cref="IOException"><paramref name="tempID"/> cannot be moved or the digest cannot be calculated.</exception>
        /// <exception cref="ImplementationAlreadyInStoreException">There is already an <see cref="Implementation"/> with the specified <paramref name="expectedDigest"/> in the store.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        protected virtual string VerifyAndAdd(string tempID, ManifestDigest expectedDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(tempID)) throw new ArgumentNullException(nameof(tempID));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            // Determine the digest method to use
            string? expectedDigestValue = expectedDigest.Best;
            if (string.IsNullOrEmpty(expectedDigestValue)) throw new NotSupportedException(Resources.NoKnownDigestMethod);

            // Determine the source and target directories
            string source = Path.Combine(DirectoryPath, tempID);
            string target = Path.Combine(DirectoryPath, expectedDigestValue);

            if (_isUnixFS) FlagUtils.ConvertToFS(source);

            // Calculate the actual digest, compare it with the expected one and create a manifest file
            VerifyDirectory(source, expectedDigest, handler).Save(Path.Combine(source, Manifest.ManifestFile));

            lock (_renameLock) // Prevent race-conditions when adding the same digest twice
            {
                if (Directory.Exists(target)) throw new ImplementationAlreadyInStoreException(expectedDigest);

                // Move directory to final store destination
                try
                {
                    Directory.Move(source, target);
                }
                catch (IOException ex)
                    // TODO: Make language independent
                    when (ex.Message.Contains("already exists"))
                {
                    throw new ImplementationAlreadyInStoreException(expectedDigest);
                }
            }

            // Prevent any further changes to the directory
            if (_useWriteProtection) EnableWriteProtection(target);
            return target;
        }
        #endregion

        #region Verify directory
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

            var generator = new ManifestGenerator(directory, format) {Tag = expectedDigest};
            handler.RunTask(generator);
            var manifest = generator.Manifest;
            string digest = manifest.CalculateDigest();

            if (digest != expectedDigestValue)
            {
                var roundedManifest = manifest.WithRoundedTimestamps();
                if (roundedManifest.CalculateDigest() == expectedDigestValue)
                {
                    Log.Info($"Expected digest {expectedDigestValue} but got {digest}. Rounding down timestamps to even numbers of seconds made the digests match.");
                    return roundedManifest;
                }
                else
                {
                    string manifestFilePath = Path.Combine(directory, Manifest.ManifestFile);
                    var expectedManifest = File.Exists(manifestFilePath) ? Manifest.Load(manifestFilePath, format) : null;
                    throw new DigestMismatchException(expectedDigestValue, digest, expectedManifest, manifest);
                }
            }

            return manifest;
        }
        #endregion

        //--------------------//

        #region List all
        /// <inheritdoc/>
        public IEnumerable<ManifestDigest> ListAll()
        {
            if (!Directory.Exists(DirectoryPath)) return Enumerable.Empty<ManifestDigest>();

            var result = new List<ManifestDigest>();
            foreach (string path in Directory.GetDirectories(DirectoryPath))
            {
                Debug.Assert(path != null);
                var digest = new ManifestDigest();
                digest.ParseID(Path.GetFileName(path));
                if (digest.Best != null) result.Add(new ManifestDigest(Path.GetFileName(path)));
            }
            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<string> ListAllTemp()
        {
            if (!Directory.Exists(DirectoryPath)) return Enumerable.Empty<string>();

            var result = new List<string>();
            foreach (string path in Directory.GetDirectories(DirectoryPath))
            {
                Debug.Assert(path != null);
                try
                {
                    // ReSharper disable once ObjectCreationAsStatement
                    new ManifestDigest(Path.GetFileName(path));
                }
                catch (NotSupportedException)
                {
                    // Anything that is not a valid digest is considered a temp directory
                    result.Add(path);
                }
            }
            return result;
        }
        #endregion

        #region Contains
        /// <inheritdoc/>
        public bool Contains(ManifestDigest manifestDigest)
            => manifestDigest.AvailableDigests.Any(digest => Directory.Exists(Path.Combine(DirectoryPath, digest)));

        /// <inheritdoc/>
        public bool Contains(string directory)
            => Directory.Exists(Path.Combine(DirectoryPath, directory));

        /// <inheritdoc/>
        public void Flush() {} // No internal caching
        #endregion

        #region Get
        /// <inheritdoc/>
        public string GetPath(ManifestDigest manifestDigest)
            => manifestDigest.AvailableDigests.Select(digest => Path.Combine(DirectoryPath, digest)).FirstOrDefault(Directory.Exists);
        #endregion

        #region Add
        /// <inheritdoc/>
        public string AddDirectory(string path, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (manifestDigest.Best == null) throw new ArgumentException("No known digest method", nameof(manifestDigest));
            #endregion

            if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);
            Log.Info($"Caching implementation {manifestDigest} in {this}");

            // Copy to temporary directory inside the cache so it can be validated safely (no manipulation of directory while validating)
            string tempDir = GetTempDir();
            try
            {
                // Copy the source directory inside the cache so it can be validated safely (no manipulation of directory while validating)
                try
                {
                    handler.RunTask(new CloneDirectory(path, tempDir) {Tag = manifestDigest});
                }
                #region Error handling
                catch (IOException ex)
                    // TODO: Make language independent
                    when (ex.Message.StartsWith("Access") && ex.Message.EndsWith("is denied."))
                {
                    throw new UnauthorizedAccessException(ex.Message, ex);
                }
                #endregion

                return VerifyAndAdd(Path.GetFileName(tempDir), manifestDigest, handler);
            }
            finally
            {
                DeleteTempDir(tempDir);
            }
        }

        /// <inheritdoc/>
        public string AddArchives(IEnumerable<ArchiveFileInfo> archiveInfos, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (archiveInfos == null) throw new ArgumentNullException(nameof(archiveInfos));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (manifestDigest.Best == null) throw new ArgumentException("No known digest method", nameof(manifestDigest));
            #endregion

            if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);
            Log.Info($"Caching implementation {manifestDigest} in {this}");

            // Extract to temporary directory inside the cache so it can be validated safely (no manipulation of directory while validating)
            string tempDir = GetTempDir();
            try
            {
                // Extract archives "over each other" in order
                foreach (var archiveInfo in archiveInfos)
                {
                    try
                    {
                        using var extractor = ArchiveExtractor.Create(archiveInfo.Path, tempDir, archiveInfo.MimeType, archiveInfo.StartOffset);
                        extractor.Extract = archiveInfo.Extract;
                        extractor.TargetSuffix = archiveInfo.Destination;
                        extractor.Tag = manifestDigest;
                        handler.RunTask(extractor);
                    }
                    #region Error handling
                    catch (IOException ex)
                    {
                        string source = archiveInfo.OriginalSource?.ToStringRfc() ?? archiveInfo.Path;
                        throw new IOException(string.Format(Resources.FailedToExtractArchive, source), ex);
                    }
                    #endregion
                }

                return VerifyAndAdd(Path.GetFileName(tempDir), manifestDigest, handler);
            }
            finally
            {
                DeleteTempDir(tempDir);
            }
        }
        #endregion

        #region Remove
        /// <inheritdoc/>
        public virtual bool Remove(ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            string path = GetPath(manifestDigest);
            if (path == null) return false;
            if (Kind == ImplementationStoreKind.ReadOnly && !WindowsUtils.IsAdministrator) throw new NotAdminException(Resources.MustBeAdminToRemove);
            if (path == Locations.InstallBase && WindowsUtils.IsWindows)
            {
                Log.Warn(Resources.NoStoreSelfRemove);
                return false;
            }
            if (WindowsUtils.IsWindowsVista)
            {
                if (!UseRestartManager(path, handler)) return false;
            }

            handler.RunTask(new SimpleTask(string.Format(Resources.DeletingImplementation, manifestDigest), () =>
            {
                DisableWriteProtection(path);

                string tempDir = Path.Combine(DirectoryPath, Path.GetRandomFileName());
                Log.Info("Attempting atomic delete: " + path);
                Directory.Move(path, tempDir);
                Directory.Delete(tempDir, recursive: true);
            }));

            return true;
        }

        /// <summary>
        /// Ensures that there are no applications running with open file handles in <paramref name="path"/>.
        /// </summary>
        /// <returns><c>true</c> if there are no longer any open file handles; <c>false</c> if this could not be ensured.</returns>
        private static bool UseRestartManager(string path, ITaskHandler handler)
        {
            try
            {
                using var restartManager = new WindowsRestartManager();

                // Look for handles to well-known executable types in the top-level directory
                // Searching for all file types and/or in subdirectories takes too long
                restartManager.RegisterResources(Directory.GetFiles(path, "*.exe"));
                restartManager.RegisterResources(Directory.GetFiles(path, "*.dll"));

                var apps = restartManager.ListApps(handler);
                if (apps.Length != 0)
                {
                    string appsList = string.Join(Environment.NewLine, apps);
                    if (handler.Ask(Resources.FilesInUse + @" " + Resources.FilesInUseAskClose + Environment.NewLine + appsList,
                        defaultAnswer: false, alternateMessage: Resources.FilesInUse + @" " + Resources.FilesInUseInform + Environment.NewLine + appsList))
                        restartManager.ShutdownApps(handler);
                    else return false;
                }
                return true;
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Warn(ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn(ex);
                return false;
            }
            catch (TimeoutException ex)
            {
                Log.Warn(ex);
                return false;
            }
            catch (Win32Exception ex)
            {
                Log.Error(ex);
                return false;
            }
            #endregion
        }
        #endregion

        #region Optimise
        /// <inheritdoc/>
        public virtual long Optimise(ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (!Directory.Exists(DirectoryPath)) return 0;
            if (Kind == ImplementationStoreKind.ReadOnly && !WindowsUtils.IsAdministrator) throw new NotAdminException(Resources.MustBeAdminToOptimise);

            using var run = new OptimiseRun(DirectoryPath);
            handler.RunTask(ForEachTask.Create(
                name: string.Format(Resources.FindingDuplicateFiles, DirectoryPath),
                target: ListAll(),
                work: run.Work));
            return run.SavedBytes;
        }
        #endregion

        #region Verify
        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public virtual void Verify(ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (!Contains(manifestDigest)) throw new ImplementationNotFoundException(manifestDigest);

            string? digest = manifestDigest.Best;
            if (digest == null) throw new NotSupportedException(Resources.NoKnownDigestMethod);
            string target = Path.Combine(DirectoryPath, digest);
            try
            {
                VerifyDirectory(target, manifestDigest, handler);

                // Reseal the directory in case the write protection got lost
                if (_useWriteProtection) EnableWriteProtection(target);
            }
            catch (DigestMismatchException ex) when (ex.ExpectedDigest != null)
            {
                Log.Info(ex.LongMessage);
                if (handler.Ask(
                    question: string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedAskRemove, ex.ExpectedDigest),
                    defaultAnswer: false, alternateMessage: string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedBatchInformation, ex.ExpectedDigest)))
                    Remove(new ManifestDigest(ex.ExpectedDigest), handler);
            }
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns <see cref="Kind"/> and <see cref="DirectoryPath"/>. Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Kind}: {DirectoryPath}";
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(DiskImplementationStore other) => other != null && DirectoryPath == other.DirectoryPath;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(DiskImplementationStore) && Equals((DiskImplementationStore)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => DirectoryPath.GetHashCode();
        #endregion
    }
}
