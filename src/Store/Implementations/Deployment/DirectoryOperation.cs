// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations.Deployment
{
    /// <summary>
    /// Common base class for deployment operations that operate on directories with <see cref="Manifests.Manifest"/>s.
    /// </summary>
    public abstract class DirectoryOperation : StagedOperation
    {
        /// <summary>
        /// The path of the directory to operate on.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The contents of a <see cref="Manifests.Manifest"/> file describing the directory.
        /// </summary>
        protected readonly Manifest Manifest;

        /// <summary>
        /// A callback object used when the the user needs to be asked questions or informed about IO tasks.
        /// </summary>
        protected readonly ITaskHandler Handler;

        /// <summary>
        /// The paths of all <see cref="ManifestNode"/>s in <see cref="Manifest"/> relative to the manifest root.
        /// </summary>
        protected readonly IReadOnlyDictionary<string, ManifestNode> ElementPaths;

        /// <summary>
        /// Creates a new manifest directory task.
        /// </summary>
        /// <param name="path">The path of the directory to operate on.</param>
        /// <param name="manifest">The contents of a <see cref="Manifests.Manifest"/> file describing the directory.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
        protected DirectoryOperation(string path, Manifest manifest, ITaskHandler handler)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));

            ElementPaths = Manifest.ListPaths();
        }

        /// <summary>
        /// Appends a random string to a file path.
        /// </summary>
        protected static string Randomize(string path) => path + "." + System.IO.Path.GetRandomFileName() + ".tmp";

        /// <summary>
        /// Indicates that applications shut down by the <see cref="WindowsRestartManager"/> shall not be restarted on <see cref="Dispose"/>.
        /// </summary>
        public bool NoRestart { get; set; }

        private WindowsRestartManager? _restartManager;

        /// <summary>
        /// Uses <see cref="WindowsRestartManager"/> to close any applications that have open references to the specified <paramref name="files"/> if possible and removes read-only attributes.
        /// </summary>
        /// <remarks>Closed applications will be restarted by <see cref="Dispose"/>.</remarks>
        protected void UnlockFiles(IEnumerable<string> files)
        {
            if (WindowsUtils.IsWindows)
            {
                var fileArray = files.ToArray();
                if (fileArray.Length == 0) return;

                if (WindowsUtils.IsWindowsVista)
                {
                    try
                    {
                        _restartManager ??= new();
                        _restartManager.RegisterResources(fileArray);
                        if (_restartManager.ListApps(Handler.CancellationToken).Length == 0)
                            NoRestart = true;
                        else
                            _restartManager.ShutdownApps(Handler);
                    }
                    #region Error handling
                    catch (IOException ex)
                    {
                        Log.Warn(ex);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warn(ex);
                    }
                    catch (TimeoutException ex)
                    {
                        Log.Warn(ex);
                    }
                    catch (Win32Exception ex)
                    {
                        Log.Error(ex);
                    }
                    #endregion
                }

                foreach (string path in fileArray)
                {
                    try
                    {
                        new FileInfo(path).IsReadOnly = false;
                    }
                    #region Error handling
                    catch (ArgumentException ex)
                    {
                        // Wrap exception since only certain exception types are allowed
                        throw new UnauthorizedAccessException(ex.Message, ex);
                    }
                    #endregion
                }
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            try
            {
                if (_restartManager != null)
                {
                    try
                    {
                        if (!NoRestart) _restartManager.RestartApps(Handler);
                        _restartManager.Dispose();
                    }
                    #region Error handling
                    catch (IOException ex)
                    {
                        Log.Warn(ex);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warn(ex);
                    }
                    catch (Win32Exception ex)
                    {
                        Log.Error(ex);
                    }
                    #endregion
                }
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
