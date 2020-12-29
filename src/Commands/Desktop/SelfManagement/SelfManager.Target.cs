// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Threading;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Commands.Desktop.SelfManagement
{
    partial class SelfManager
    {
        /// <summary>
        /// Loads the <see cref="Manifest"/> file in a directory.
        /// </summary>
        /// <param name="dirPath">The directory to check for a manifest file.</param>
        /// <returns>The loaded <see cref="Manifest"/>.</returns>
        private Manifest LoadManifest(string dirPath)
        {
            string manifestPath = Path.Combine(dirPath, Manifest.ManifestFile);
            if (File.Exists(manifestPath))
                return Manifest.Load(manifestPath, ManifestFormat.Sha256New);
            else if (Directory.Exists(dirPath) && Directory.GetFileSystemEntries(dirPath).Length != 0)
            {
                Log.Info($"No .manifest file found in '{dirPath}'. Assuming directory is clean.");
                var generator = new ManifestGenerator(dirPath, ManifestFormat.Sha256New);
                Handler.RunTask(generator);
                return generator.Manifest;
            }
            else
                return new Manifest(ManifestFormat.Sha256);
        }

        /// <summary>A mutex that prevents Zero Install instances from being launched while an update is in progress.</summary>
        private AppMutex? _targetMutex;

        /// <summary>
        /// Waits for any Zero Install instances running in <see cref="TargetDir"/> to terminate and then prevents new ones from starting.
        /// </summary>
        /// <remarks>The <see cref="TargetDir"/> is encoded into an <see cref="AppMutex"/> name using <see cref="object.GetHashCode"/>.</remarks>
        private void TargetMutexAcquire()
        {
            if (TargetDir == Locations.InstallBase)
            {
                Log.Info("Cannot use Mutex because source and target directory are the same: " + TargetDir);
                return;
            }

            int hashCode = TargetDir.GetHashCode();
            if (hashCode == Locations.InstallBase.GetHashCode())
            { // Very unlikely but possible, since .GetHashCode() is not a cryptographic hash
                Log.Warn("Hash collision between " + TargetDir + " and " + Locations.InstallBase + "! Not using Mutex.");
                return;
            }
            string targetMutex = "mutex-" + hashCode;

            Handler.RunTask(new SimpleTask(Resources.MutexWait, () =>
            {
                // Wait for existing instances to terminate
                while (AppMutex.Probe(targetMutex))
                    Thread.Sleep(1000);

                // Prevent new instances from starting
                _targetMutex = AppMutex.Create(targetMutex + "-update");

                // Detect any new instances that started in the short time between detecting existing ones and blocking new ones
                while (AppMutex.Probe(targetMutex))
                    Thread.Sleep(1000);
            }));
        }

        /// <summary>
        /// Counterpart to <see cref="TargetMutexAcquire"/>.
        /// </summary>
        private void TargetMutexRelease() => _targetMutex?.Close();

        /// <summary>
        /// Try to remove OneGet Bootstrap module to prevent future PowerShell sessions from loading it again.
        /// </summary>
        private void RemoveOneGetBootstrap()
        {
            RemoveOneGetBootstrap(Path.Combine(
                Environment.GetFolderPath(MachineWide ? Environment.SpecialFolder.ProgramFiles : Environment.SpecialFolder.MyDocuments),
                "PackageManagement", "ProviderAssemblies", "0install"));
            RemoveOneGetBootstrap(Path.Combine(
                Environment.GetFolderPath(MachineWide ? Environment.SpecialFolder.ProgramFiles : Environment.SpecialFolder.LocalApplicationData),
                "WindowsPowerShell", "Modules", "0install"));
        }

        private static void RemoveOneGetBootstrap(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return;

            try
            {
                foreach (string subDirPath in Directory.GetDirectories(dirPath))
                {
                    foreach (string filePath in Directory.GetFiles(subDirPath))
                    {
                        string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                        Log.Debug($"Trying to move '{filePath}' to '{tempPath}' to prevent future PowerShell sessions from loading it again");
                        File.Move(filePath, tempPath);
                    }
                    Directory.Delete(subDirPath);
                }
                Directory.Delete(dirPath);
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Debug(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Debug(ex);
            }
            #endregion
        }
    }
}
