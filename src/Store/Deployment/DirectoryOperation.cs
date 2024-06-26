// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Deployment;

/// <summary>
/// Common base class for deployment operations that operate on directories with <see cref="Manifests.Manifest"/>s.
/// </summary>
/// <param name="path">The path of the directory to operate on.</param>
/// <param name="manifest">The contents of a &lt;see cref="Manifests.Manifest"/&gt; file describing the directory.</param>
/// <param name="handler">A callback object used when the user needs to be asked questions or informed about IO tasks.</param>
[MustDisposeResource]
public abstract class DirectoryOperation(string path, Manifest manifest, ITaskHandler handler) : StagedOperation
{
    /// <summary>
    /// The path of the directory to operate on.
    /// </summary>
    protected readonly string Path = path;

    /// <summary>
    /// The contents of a <see cref="Manifests.Manifest"/> file describing the directory.
    /// </summary>
    protected readonly Manifest Manifest = manifest;

    /// <summary>
    /// A callback object used when the user needs to be asked questions or informed about IO tasks.
    /// </summary>
    protected readonly ITaskHandler Handler = handler;

    /// <summary>
    /// Appends a random string to a file path.
    /// </summary>
    protected static string Randomize(string path) => $"{path}.{System.IO.Path.GetRandomFileName()}.tmp";

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
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
                {
                    Log.Warn(string.Format(Resources.FailedToUnlockFiles, Path), ex);
                }
                catch (Win32Exception ex)
                {
                    Log.Error("Problem using Windows Restart Manager", ex);
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
            if (WindowsUtils.IsWindowsVista && _restartManager != null)
            {
                try
                {
                    if (!NoRestart) _restartManager.RestartApps(Handler);
                    _restartManager.Dispose();
                }
                #region Error handling
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
                {
                    Log.Warn("Failed to restart previously closed apps", ex);
                }
                catch (Win32Exception ex)
                {
                    Log.Error("Problem using Windows Restart Manager", ex);
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
