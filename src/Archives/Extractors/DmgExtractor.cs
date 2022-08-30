// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Apple Disk images (.dmg).
/// </summary>
public class DmgExtractor : ArchiveExtractor
{
    /// <summary>
    /// Creates a new .dmg extractor.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
    /// <exception cref="PlatformNotSupportedException">The current platform is not macOS.</exception>
    public DmgExtractor(ITaskHandler handler) : base(handler)
    {
        if (!UnixUtils.IsMacOSX) throw new PlatformNotSupportedException(Resources.ExtractionOnlyOnMacOS);
    }

    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        EnsureFile(stream, archivePath =>
        {
            var launcher = new ProcessLauncher("hdiutil");
            using var tempDir = new TemporaryDirectory("0install-archive");

            try
            {
                launcher.Run("attach", "-quiet", "-readonly", "-mountpoint", tempDir, "-nobrowse", archivePath);
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
                        string extractDir = Path.Combine(tempDir, subDir);
                        if (Directory.Exists(extractDir))
                            Handler.RunTask(new ReadDirectory(extractDir, builder) {Tag = Tag});
                    }
                }
                finally
                {
                    launcher.Run("detach", "-quiet", tempDir);
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
