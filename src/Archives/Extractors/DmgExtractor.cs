// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Apple Disk images (.dmg).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <exception cref="PlatformNotSupportedException">The current platform is not macOS.</exception>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class DmgExtractor(ITaskHandler handler)
    : ArchiveExtractor(UnixUtils.IsMacOSX ? handler : throw new PlatformNotSupportedException(Resources.ExtractionOnlyOnMacOS))
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        EnsureFile(stream, archivePath =>
        {
            using var tempDir = new TemporaryDirectory("0install-archive");

            try
            {
                Attach(archivePath, tempDir);
                try
                {
                    if (subDir == null)
                    {
                        Handler.RunTask(new ReadDirectory(tempDir, builder) {Tag = Tag});
                        try
                        {
                            builder.Remove(".Trashes");
                        }
                        catch (IOException)
                        {}
                    }
                    else
                    {
                        string extractDir = Paths.Combine(tempDir, subDir);
                        if (Directory.Exists(extractDir))
                            Handler.RunTask(new ReadDirectory(extractDir, builder) {Tag = Tag});
                    }
                }
                finally
                {
                    Detach(tempDir);
                }
            }
            #region Error handling
            catch (ExitCodeException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        });
    }

    private static readonly bool _useDiskutil =
#if NET
        OperatingSystem.IsMacOSVersionAtLeast(15);
#else
        false;
#endif

    private static void Attach(string archivePath, string mountPoint)
    {
        if (_useDiskutil) new ProcessLauncher("diskutil").Run("image", "attach", "--readOnly", "--nobrowse", "--mountPoint", mountPoint, archivePath);
        else new ProcessLauncher("hdiutil").Run("attach", "-quiet", "-readonly", "-mountpoint", mountPoint, "-nobrowse", archivePath);
    }

    private static void Detach(string mountPoint)
    {
        if (_useDiskutil) new ProcessLauncher("diskutil").Run("eject", mountPoint);
        else new ProcessLauncher("hdiutil").Run("detach", "-quiet", mountPoint);
    }
}
