// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store.Implementations.Archives;

namespace ZeroInstall.Publish.Capture
{
    /// <summary>
    /// Manages the process of taking two <see cref="Snapshot"/>s and comparing them to generate a <see cref="Feed"/>.
    /// </summary>
    public class CaptureSession
    {
        private readonly Snapshot _snapshot;

        private readonly FeedBuilder _feedBuilder;

        /// <summary>
        /// The fully qualified path to the installation directory; leave <c>null</c> or empty for auto-detection.
        /// </summary>
        public string? InstallationDir { get; set; }

        private CaptureSession(Snapshot snapshotBefore, FeedBuilder feedBuilder)
        {
            _snapshot = snapshotBefore;
            _feedBuilder = feedBuilder;
        }

        /// <summary>
        /// Captures the current system state as a snapshot of the system state before the target application was installed.
        /// </summary>
        /// <param name="feedBuilder">All collected data is stored into this builder. You can perform additional modifications before using <see cref="FeedBuilder.Build"/> to get a feed.</param>
        /// <exception cref="IOException">There was an error accessing the registry or file system.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the registry or the file system was not permitted.</exception>
        public static CaptureSession Start(FeedBuilder feedBuilder)
        {
            #region Sanity checks
            if (feedBuilder == null) throw new ArgumentNullException(nameof(feedBuilder));
            #endregion

            return new CaptureSession(Snapshot.Take(), feedBuilder);
        }

        private SnapshotDiff? _diff;

        /// <summary>
        /// Collects data from the locations indicated by the differences between the <see cref="Start"/> state and the current system state.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        /// <exception cref="InvalidOperationException">No installation directory was detected.</exception>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">There was an error accessing the registry or file system.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the registry or file system was not permitted.</exception>
        public void Diff(ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            _diff = new SnapshotDiff(before: _snapshot, after: Snapshot.Take());
            if (string.IsNullOrEmpty(InstallationDir)) InstallationDir = _diff.GetInstallationDir();

            _feedBuilder.ImplementationDirectory = InstallationDir;
            _feedBuilder.DetectCandidates(handler);
        }

        /// <summary>
        /// Finishes the capture process after <see cref="Diff"/> has been called an <see cref="FeedBuilder.MainCandidate"/> has been set.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Diff"/> was not called or <see cref="FeedBuilder.MainCandidate"/> is not set.</exception>
        /// <exception cref="IOException">There was an error accessing the registry or file system.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the registry or file system was not permitted.</exception>
        public void Finish()
        {
            if (_diff == null || InstallationDir == null) throw new InvalidOperationException("Diff() must be called first.");

            _feedBuilder.GenerateCommands();

            var commandMapper = new CommandMapper(InstallationDir, _feedBuilder.Commands);
            _feedBuilder.CapabilityList = GetCapabilityList(commandMapper, _diff);
        }

        private static CapabilityList GetCapabilityList(CommandMapper commandMapper, SnapshotDiff diff)
        {
            var capabilities = new CapabilityList {OS = OS.Windows};
            string appName = null, appDescription = null;

            diff.CollectFileTypes(commandMapper, capabilities);
            diff.CollectContextMenus(commandMapper, capabilities);
            diff.CollectAutoPlays(commandMapper, capabilities);
            diff.CollectDefaultPrograms(commandMapper, capabilities, ref appName);

            var appRegistration = diff.GetAppRegistration(commandMapper, capabilities, ref appName, ref appDescription);
            if (appRegistration != null) capabilities.Entries.Add(appRegistration);
            else
            { // Only collect URL protocols if there wasn't already an application registration that covered them
                diff.CollectProtocolAssocs(commandMapper, capabilities);
            }

            return capabilities;
        }

        /// <summary>
        /// Creates a archive containing the <see cref="InstallationDir"/>.
        /// </summary>
        /// <remarks>Sets <see cref="FeedBuilder.RetrievalMethod"/> and calls <see cref="FeedBuilder.GenerateDigest"/>.</remarks>
        /// <param name="archivePath">The path of the archive file to create.</param>
        /// <param name="archiveUrl">The URL where the archive will be uploaded.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        /// <exception cref="InvalidOperationException"><see cref="Diff"/> was not called or <see cref="FeedBuilder.MainCandidate"/> is not set.</exception>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">There was an error reading the installation files or writing the archive.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the file system was not permitted.</exception>
        public void CollectFiles(string archivePath, Uri archiveUrl, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(archivePath)) throw new ArgumentNullException(nameof(archivePath));
            if (archiveUrl == null) throw new ArgumentNullException(nameof(archiveUrl));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (InstallationDir == null) throw new InvalidOperationException("Diff() must be called first.");

            _feedBuilder.ImplementationDirectory = InstallationDir;
            _feedBuilder.GenerateDigest(handler);

            string mimeType = Archive.GuessMimeType(archivePath) ?? Archive.MimeTypeZip;
            using (var generator = ArchiveGenerator.Create(InstallationDir, archivePath, mimeType))
                handler.RunTask(generator);
            _feedBuilder.RetrievalMethod = new Archive {Href = archiveUrl, MimeType = mimeType, Size = new FileInfo(archivePath).Length};
        }

        #region Storage
        /// <summary>
        /// Loads a capture session from a snapshot file.
        /// </summary>
        /// <param name="path">The file to load from.</param>
        /// <param name="feedBuilder">All collected data is stored into this builder. You can perform additional modifications before using <see cref="FeedBuilder.Build"/> to get a feed.</param>
        /// <exception cref="IOException">A problem occurred while reading the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the binary data.</exception>
        public static CaptureSession Load(string path, FeedBuilder feedBuilder)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (feedBuilder == null) throw new ArgumentNullException(nameof(feedBuilder));
            #endregion

            return new CaptureSession(BinaryStorage.LoadBinary<Snapshot>(path), feedBuilder);
        }

        /// <summary>
        /// Saves the capture session to a snapshot file.
        /// </summary>
        /// <param name="path">The file to save in.</param>
        /// <exception cref="IOException">A problem occurred while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
        public void Save(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            _snapshot.SaveBinary(path);
        }
        #endregion
    }
}
