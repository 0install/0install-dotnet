// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using WixToolset.Dtf.Compression.Cab;
using WixToolset.Dtf.WindowsInstaller;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Windows Installer packages (.msi) with one or more embedded CAB archives.
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <exception cref="PlatformNotSupportedException">The current platform is not Windows.</exception>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class MsiExtractor(ITaskHandler handler)
    : ArchiveExtractor(WindowsUtils.IsWindows ? handler : throw new PlatformNotSupportedException(Resources.ExtractionOnlyOnWindows))
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        EnsureFile(stream, msiPath =>
        {
            try
            {
                using var engine = new CabEngine();

                using var package = new MsiPackage(msiPath);
                package.ForEachCabinet(cabStream =>
                {
                    Handler.CancellationToken.ThrowIfCancellationRequested();

                    EnsureSeekable(cabStream, seekableStream =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        engine.Unpack(
                            new CabExtractorContext(builder, seekableStream, x => NormalizePath(package.Files[x], subDir), Handler.CancellationToken),
                            fileFilter: package.Files.ContainsKey);
                    });
                });
            }
            #region Error handling
            catch (Exception ex) when (ex is InstallerException or CabException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        });
    }
}
