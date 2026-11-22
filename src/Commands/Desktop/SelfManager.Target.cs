// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Commands.Desktop;

partial class SelfManager
{
    /// <summary>
    /// Loads the <see cref="Manifest"/> file in a directory.
    /// </summary>
    /// <param name="dirPath">The directory to check for a manifest file.</param>
    /// <returns>The loaded <see cref="Manifest"/>.</returns>
    private Manifest LoadManifest(string dirPath)
    {
        // Manifest in directory
        string manifestPath = Paths.Combine(dirPath, Manifest.ManifestFile);
        if (File.Exists(manifestPath)) return Manifest.Load(manifestPath, ManifestFormat.Sha256New);

        // Manifest in parent directory
        manifestPath = Paths.Combine(dirPath, "..", Manifest.ManifestFile);
        if (File.Exists(manifestPath)) return Manifest.Load(manifestPath, ManifestFormat.Sha256New).ScopedTo(Paths.FileName(dirPath));

        // Existing directory with no manifest
        if (Directory.Exists(dirPath) && Directory.GetFileSystemEntries(dirPath).Length != 0)
        {
            Log.Info($"No .manifest file found in '{dirPath}'. Assuming directory is clean.");
            var builder = new ManifestBuilder(ManifestFormat.Sha256New);
            Handler.RunTask(new ReadDirectory(dirPath, builder));
            return builder.Manifest;
        }

        // No existing directory
        return new Manifest(ManifestFormat.Sha256);
    }

    /// <summary>A mutex that prevents Zero Install instances from being launched while an update is in progress.</summary>
    private AppMutex? _updateMutex, _legacyUpdateMutex;

    /// <summary>
    /// Waits for any Zero Install instances running in <see cref="TargetDir"/> to terminate and then prevents new ones from starting.
    /// </summary>
    /// <remarks>The <see cref="TargetDir"/> is encoded into an <see cref="AppMutex"/> name using <see cref="object.GetHashCode"/>.</remarks>
    private void MutexAcquire()
    {
        if (Paths.AreEqual(TargetDir, ZeroInstallInstance.DeploymentRoot))
        {
            Log.Info($"Cannot use Mutex because source and target directory are the same: {TargetDir}");
            return;
        }

        // The target has the same layout as the source (enforced by Deploy()), but may not have been deployed yet
        string targetInstallBase = ZeroInstallDeployment.GetBinaryDir(TargetDir, appBundle: ZeroInstallInstance.IsAppBundle);

        if (ZeroInstallEnvironment.MutexName(targetInstallBase) == ZeroInstallEnvironment.MutexName(Locations.InstallBase))
        {
            Log.Warn($"Hash collision between {targetInstallBase} and {Locations.InstallBase}! Not using Mutex.");
            return;
        }

        Handler.RunTask(new ActionTask(Resources.MutexWait, () =>
        {
            // Wait for existing instances to terminate
            while (AppMutex.Probe(ZeroInstallEnvironment.MutexName(targetInstallBase)) || AppMutex.Probe(ZeroInstallEnvironment.LegacyMutexName(targetInstallBase)))
            {
                Thread.Sleep(1000);
                Handler.CancellationToken.ThrowIfCancellationRequested();
            }

            // Prevent new instances from starting during the update
            _updateMutex = AppMutex.Create(ZeroInstallEnvironment.UpdateMutexName(targetInstallBase));
            _legacyUpdateMutex = AppMutex.Create(ZeroInstallEnvironment.LegacyUpdateMutexName(targetInstallBase));

            // Detect any new instances that started in the short time between detecting existing ones and blocking new ones
            while (AppMutex.Probe(ZeroInstallEnvironment.MutexName(targetInstallBase)) || AppMutex.Probe(ZeroInstallEnvironment.LegacyMutexName(targetInstallBase)))
            {
                Thread.Sleep(1000);
                Handler.CancellationToken.ThrowIfCancellationRequested();
            }
        }));
    }

    /// <summary>
    /// Counterpart to <see cref="MutexAcquire"/>.
    /// </summary>
    private void MutexRelease()
    {
        _updateMutex?.Dispose();
        _legacyUpdateMutex?.Dispose();
    }
}
