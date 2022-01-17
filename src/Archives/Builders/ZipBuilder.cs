// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Streams;
using ZeroInstall.Archives.Extractors;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds a ZIP archive (.zip).
/// </summary>
public class ZipBuilder : IArchiveBuilder
{
    private readonly ZipOutputStream _zipStream;

    /// <summary>
    /// Creates a ZIP archive builder.
    /// </summary>
    /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
    public ZipBuilder(Stream stream)
    {
        _zipStream = new(stream ?? throw new ArgumentNullException(nameof(stream)));
    }

    public void Dispose()
        => _zipStream.Dispose();

    /// <inheritdoc/>
    public void AddDirectory(string path)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        _zipStream.PutNextEntry(new ZipEntry(path.ToUnixPath() + '/'));
    }

    /// <inheritdoc/>
    public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        #endregion

        var entry = new ZipEntry(path.ToUnixPath())
        {
            Size = stream.Length,
            DateTime = modifiedTime,
            HostSystem = (int)HostSystemID.Unix,
            ExtraData = GetUnixTimestamp(modifiedTime)
        };
        if (executable)
            entry.ExternalFileAttributes = ZipExtractor.DefaultAttributes | ZipExtractor.ExecuteAttributes;
        _zipStream.PutNextEntry(entry);
        stream.CopyToEx(_zipStream);
    }

    /// <summary>
    /// Encodes a <paramref name="timestamp"/> as a <see cref="ZipEntry.ExtraData"/> in a format that ensures it will be read for <see cref="ZipEntry.DateTime"/>, preserving second-accuracy.
    /// </summary>
    private static byte[] GetUnixTimestamp(DateTime timestamp)
    {
        var extraData = new ZipExtraData();
        extraData.AddEntry(new ExtendedUnixData
        {
            AccessTime = timestamp,
            CreateTime = timestamp,
            ModificationTime = timestamp,
            Include = ExtendedUnixData.Flags.AccessTime | ExtendedUnixData.Flags.CreateTime | ExtendedUnixData.Flags.ModificationTime
        });
        return extraData.GetEntryData();
    }

    /// <inheritdoc/>
    public void AddSymlink(string path, string target)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        if (target == null) throw new ArgumentNullException(nameof(target));
        #endregion

        var data = target.ToUnixPath().ToStream();
        _zipStream.PutNextEntry(new ZipEntry(path.ToUnixPath())
        {
            Size = data.Length,
            HostSystem = (int)HostSystemID.Unix,
            ExternalFileAttributes = ZipExtractor.DefaultAttributes | ZipExtractor.SymlinkAttributes
        });
        data.WriteTo(_zipStream);
    }

    /// <inheritdoc/>
    public void AddHardlink(string path, string target, bool executable = false)
        => throw new NotSupportedException("ZIP archives do not support hardlinks.");
}
