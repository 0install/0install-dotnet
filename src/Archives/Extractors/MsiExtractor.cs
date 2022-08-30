// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using Microsoft.Deployment.Compression.Cab;
using Microsoft.Deployment.WindowsInstaller;
using NanoByte.Common.Native;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Windows Installer packages (.msi) with one or more embedded CAB archives.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class MsiExtractor : ArchiveExtractor
{
    /// <summary>
    /// Creates an MSI extractor.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
    /// <exception cref="PlatformNotSupportedException">The current platform is not Windows.</exception>
    public MsiExtractor(ITaskHandler handler)
        : base(handler)
    {
        if (!WindowsUtils.IsWindows) throw new PlatformNotSupportedException(Resources.ExtractionOnlyOnWindows);
    }

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
#endif
