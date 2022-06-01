// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Streams;
using NanoByte.Common.Values;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts ZIP archives (.zip).
/// </summary>
[PrimaryConstructor]
public partial class ZipExtractor : ArchiveExtractor
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        stream = stream.WithSeekBuffer(bufferSize: 2 * 1024 * 1024);

        try
        {
            ExtractFiles(new ZipInputStream(stream) { IsStreamOwner = false }, subDir, builder);
        }
        catch (Exception ex) when (ex is SharpZipBaseException or InvalidDataException or ArgumentOutOfRangeException)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(Resources.ArchiveInvalid, ex);
        }

        try
        {
            ApplyCentral(new ZipFile(stream, leaveOpen: true), subDir, builder);
        }
        catch (Exception ex)
        {
            Log.Warn("Unable to process ZIP central directory", ex);
        }
    }

    private void ExtractFiles(ZipInputStream zipStream, string? subDir, IBuilder builder)
    {
        ZipEntry entry;
        while ((entry = zipStream.GetNextEntry()) != null)
        {
            Handler.CancellationToken.ThrowIfCancellationRequested();

            string? relativePath = NormalizePath(entry.Name, subDir);
            if (string.IsNullOrEmpty(relativePath)) continue;

            if (entry.IsDirectory)
                builder.AddDirectory(relativePath);
            else if (entry.IsFile)
                builder.AddFile(relativePath, zipStream.WithLength(entry.Size), GetTimestamp(entry));
        }
    }

    private static UnixTime GetTimestamp(ZipEntry entry)
    {
        var oldTimestamp = new ZipExtraData(entry.ExtraData).GetData<OldUnixExtraData>()?.ModificationTime;
        if (oldTimestamp.HasValue) return oldTimestamp.Value;

        // Special-case handling for unset/default timestamps to match behavior of Info-ZIP
        if (entry.DateTime == new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            return entry.DateTime - TimeSpan.FromDays(1);

        return entry.DateTime;
    }

    private static void ApplyCentral(ZipFile zipFile, string? subDir, IBuilder builder)
    {
        for (int i = 0; i < zipFile.Count; i++)
        {
            var entry = zipFile[i];

            string? relativePath = NormalizePath(entry.Name, subDir);
            if (string.IsNullOrEmpty(relativePath)) continue;

            if (entry.IsDirectory)
                builder.AddDirectory(relativePath);
            else if (entry.IsFile)
            {
                if (IsSymlink(entry))
                    builder.TurnIntoSymlink(relativePath);
                else if (IsExecutable(entry))
                    builder.MarkAsExecutable(relativePath);
            }
        }
    }

    /// <summary>
    /// The default <see cref="ZipEntry.ExternalFileAttributes"/>.
    /// </summary>
    public const int DefaultAttributes = (6 << 22) + (4 << 19) + (4 << 16); // Octal: 644

    /// <summary>
    /// The <see cref="ZipEntry.ExternalFileAttributes"/> that indicate a ZIP entry is a symlink.
    /// </summary>
    public const int SymlinkAttributes = 4 << 27; // Octal: 40000

    /// <summary>
    /// Determines whether a <see cref="ZipEntry"/> was created on a Unix-system with the symlink flag set.
    /// </summary>
    private static bool IsSymlink(ZipEntry entry)
        => (entry.HostSystem == (int)HostSystemID.Unix)
        && entry.ExternalFileAttributes.HasFlag(SymlinkAttributes);

    /// <summary>
    /// The <see cref="ZipEntry.ExternalFileAttributes"/> that indicate a ZIP entry is an executable file.
    /// </summary>
    public const int ExecuteAttributes = (1 << 22) + (1 << 19) + (1 << 16); // Octal: 111

    /// <summary>
    /// Determines whether a <see cref="ZipEntry"/> was created on a Unix-system with the executable flag set.
    /// </summary>
    private static bool IsExecutable(ZipEntry entry)
        => (entry.HostSystem == (int)HostSystemID.Unix)
        && (entry.ExternalFileAttributes & ExecuteAttributes) > 0; // Check if anybody is allowed to execute
}
