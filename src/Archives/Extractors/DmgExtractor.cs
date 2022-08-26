// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Apple Disk images (.dmg).
/// </summary>
[PrimaryConstructor]
public partial class DmgExtractor : ArchiveExtractor
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        if (!UnixUtils.IsMacOSX) throw new NotSupportedException(Resources.ExtractionOnlyOnMacOS);

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
