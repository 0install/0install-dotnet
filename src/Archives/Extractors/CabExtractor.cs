// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using NanoByte.Common.Native;
using WixToolset.Dtf.Compression.Cab;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts MS Cabinets (.cab).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <exception cref="PlatformNotSupportedException">The current platform is not Windows.</exception>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class CabExtractor(ITaskHandler handler)
    : ArchiveExtractor(WindowsUtils.IsWindows ? handler : throw new PlatformNotSupportedException(Resources.ExtractionOnlyOnWindows))
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        using var engine = new CabEngine();

        EnsureSeekable(stream, seekableStream =>
        {
            try
            {
                engine.Unpack(
                    new CabExtractorContext(builder, seekableStream, path => NormalizePath(path, subDir), Handler.CancellationToken),
                    fileFilter: path => NormalizePath(path, subDir) != null);
            }
            #region Error handling
            catch (CabException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        });
    }
}
#endif
