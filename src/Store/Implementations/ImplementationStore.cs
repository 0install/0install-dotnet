// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Manages a directory that stores implementations. Also known as an implementation cache.
    /// </summary>
    public partial class ImplementationStore : ImplementationSink, IImplementationStore, IEquatable<ImplementationStore>
    {
        /// <inheritdoc/>
        public ImplementationStoreKind Kind => ReadOnly ? ImplementationStoreKind.ReadOnly : ImplementationStoreKind.ReadWrite;

        /// <summary>
        /// Creates a new implementation store using a specific path to a directory.
        /// </summary>
        /// <param name="path">A fully qualified directory path. The directory will be created if it doesn't exist yet.</param>
        /// <param name="useWriteProtection">Controls whether implementation directories are made write-protected once added to the store to prevent unintentional modification (which would invalidate the manifest digests).</param>
        /// <exception cref="IOException">The <paramref name="path"/> could not be created or the underlying filesystem can not store file-changed times accurate to the second.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating the <paramref name="path"/> is not permitted.</exception>
        public ImplementationStore(string path, bool useWriteProtection = true)
            : base(path, useWriteProtection)
        {}

        /// <summary>
        /// Determines whether the store contains a local copy of an implementation identified by a specific <see cref="ManifestDigest"/>.
        /// </summary>
        /// <param name="manifestDigest">The digest of the implementation to check for.</param>
        /// <returns>
        ///   <c>true</c> if the specified implementation is available in the store;
        ///   <c>false</c> if the specified implementation is not available in the store or if read access to the store is not permitted.
        /// </returns>
        /// <remarks>If read access to the store is not permitted, no exception is thrown.</remarks>
        public bool Contains(ManifestDigest manifestDigest)
            => manifestDigest.AvailableDigests.Any(digest => Directory.Exists(System.IO.Path.Combine(Path, digest)));

        /// <inheritdoc/>
        public string? GetPath(ManifestDigest manifestDigest)
            => manifestDigest.AvailableDigests.Select(digest => System.IO.Path.Combine(Path, digest)).FirstOrDefault(Directory.Exists);

        /// <inheritdoc/>
        public IEnumerable<ManifestDigest> ListAll()
        {
            if (!Directory.Exists(Path)) return Enumerable.Empty<ManifestDigest>();

            var result = new List<ManifestDigest>();
            foreach (string path in Directory.GetDirectories(Path))
            {
                var digest = new ManifestDigest();
                digest.ParseID(System.IO.Path.GetFileName(path));
                if (digest.Best != null) result.Add(new ManifestDigest(System.IO.Path.GetFileName(path)));
            }
            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<string> ListAllTemp()
        {
            if (!Directory.Exists(Path)) return Enumerable.Empty<string>();

            var result = new List<string>();
            foreach (string path in Directory.GetDirectories(Path))
            {
                try
                {
                    // ReSharper disable once ObjectCreationAsStatement
                    new ManifestDigest(System.IO.Path.GetFileName(path));
                }
                catch (NotSupportedException)
                {
                    // Anything that is not a valid digest is considered a temp directory
                    result.Add(path);
                }
            }
            return result;
        }

        /// <inheritdoc/>
        public bool Remove(ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            string? path = GetPath(manifestDigest);
            if (path == null) return false;
            if (ReadOnly && !WindowsUtils.IsAdministrator) throw new NotAdminException(Resources.MustBeAdminToRemove);
            if (path == Locations.InstallBase && WindowsUtils.IsWindows)
            {
                Log.Warn(Resources.NoStoreSelfRemove);
                return false;
            }
            if (WindowsUtils.IsWindowsVista)
            {
                if (!UseRestartManager(path, handler)) return false;
            }

            Log.Info(string.Format(Resources.DeletingImplementation, manifestDigest));

            DisableWriteProtection(path);
            string tempDir = System.IO.Path.Combine(Path, System.IO.Path.GetRandomFileName());
            Directory.Move(path, tempDir);
            Directory.Delete(tempDir, recursive: true);

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

                var apps = restartManager.ListApps(handler.CancellationToken);
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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
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

        /// <inheritdoc/>
        public void Verify(ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (!Contains(manifestDigest)) throw new ImplementationNotFoundException(manifestDigest);

            string? digest = manifestDigest.Best;
            if (digest == null) throw new NotSupportedException(Resources.NoKnownDigestMethod);
            string target = System.IO.Path.Combine(Path, digest);
            try
            {
                VerifyDirectory(target, manifestDigest, handler);

                // Reseal the directory in case the write protection got lost
                if (UseWriteProtection) EnableWriteProtection(target);
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

        /// <inheritdoc/>
        public long Optimise(ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (!Directory.Exists(Path)) return 0;
            if (ReadOnly && !WindowsUtils.IsAdministrator) throw new NotAdminException(Resources.MustBeAdminToOptimise);

            using var run = new OptimiseRun(Path);
            handler.RunTask(ForEachTask.Create(
                name: string.Format(Resources.FindingDuplicateFiles, Path),
                target: ListAll(),
                work: run.Work));
            return run.SavedBytes;
        }

        /// <summary>
        /// Creates string representation suitable for console output.
        /// </summary>
        public override string ToString()
            => $"{Kind}: {Path}";

        /// <inheritdoc/>
        public bool Equals(ImplementationStore? other)
            => other != null
            && Path == other.Path;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(ImplementationStore) && Equals((ImplementationStore)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => Path.GetHashCode();
    }
}
