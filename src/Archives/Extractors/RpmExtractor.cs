// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;
using static System.Runtime.InteropServices.RuntimeInformation;
using static System.Runtime.InteropServices.OSPlatform;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts RPM packages (.rpm).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <exception cref="PlatformNotSupportedException">The current platform is not Linux.</exception>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class RpmExtractor(ITaskHandler handler)
    : ArchiveExtractor(IsOSPlatform(Linux) ? handler : throw new PlatformNotSupportedException(Resources.ExtractionOnlyOnLinux))
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        EnsureFile(stream, archivePath =>
        {
            var launcher = new ProcessLauncher("sh");
            using var tempDir = new TemporaryDirectory("0install-archive");

            try
            {
                // Use shell to pipe rpm2cpio output to cpio for extraction
                launcher.Run("-c", $"cd '{tempDir}' && rpm2cpio '{archivePath}' | cpio -idm");

                if (subDir == null)
                    Handler.RunTask(new ReadDirectory(tempDir, builder) {Tag = Tag});
                else
                {
                    string extractDir = Path.Combine(tempDir, subDir);
                    if (Directory.Exists(extractDir))
                        Handler.RunTask(new ReadDirectory(extractDir, builder) {Tag = Tag});
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
}
