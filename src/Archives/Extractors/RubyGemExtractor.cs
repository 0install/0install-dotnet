// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Tar;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts Ruby Gem archives (.gem).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class RubyGemExtractor(ITaskHandler handler) : TarGzExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        try
        {
            var tarStream = new TarInputStream(stream, Encoding.UTF8) {IsStreamOwner = false};
            while (true)
            {
                Handler.CancellationToken.ThrowIfCancellationRequested();

                var entry = tarStream.GetNextEntry() ?? throw new IOException(Resources.ArchiveInvalid);
                if (entry.Name == "data.tar.gz")
                {
                    base.Extract(builder, tarStream, subDir);
                    return;
                }
            }
        }
        #region Error handling
        catch (Exception ex) when (ex is SharpZipBaseException or InvalidDataException or ArgumentOutOfRangeException)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(Resources.ArchiveInvalid, ex);
        }
        #endregion
    }
}
