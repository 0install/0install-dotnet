// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Models information about a temporary directory in an <see cref="IImplementationStore"/> for display in a UI.
    /// </summary>
    public sealed class TempDirectoryNode : StoreNode
    {
        /// <summary>
        /// Creates a new temporary directory node.
        /// </summary>
        /// <param name="path">The path of the directory in the store.</param>
        /// <param name="implementationStore">The <see cref="IImplementationStore"/> the directory is located in.</param>
        /// <exception cref="FormatException">The manifest file is not valid.</exception>
        /// <exception cref="IOException">The manifest file could not be read.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        public TempDirectoryNode(string path, IImplementationStore implementationStore)
            : base(implementationStore)
        {
            #region Sanity checks
            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            #endregion

            Path = path;
        }

        /// <inheritdoc/>
        public override string Name
        {
            get => Resources.TemporaryDirectories + Named.TreeSeparator + System.IO.Path.GetFileName(Path) + (SuffixCounter == 0 ? "" : " " + SuffixCounter);
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override string Path { get; }

        /// <summary>
        /// Deletes this temporary directory from the <see cref="IImplementationStore"/> it is located in.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
        /// <exception cref="DirectoryNotFoundException">The directory could be found in the store.</exception>
        /// <exception cref="IOException">The directory could not be deleted.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
        public override void Delete(ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            handler.RunTask(new SimpleTask(
                string.Format(Resources.DeletingDirectory, Path),
                () =>
                {
                    DiskImplementationStore.DisableWriteProtection(Path);
                    Directory.Delete(Path, recursive: true);
                }));
        }
    }
}
