// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Pipelines;
using NanoByte.Common.Streams;
using WixToolset.Dtf.Compression;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Used to hold state while extracting a MS Cabinet (.cab).
/// </summary>
[PrimaryConstructor]
internal sealed partial class CabExtractorContext : IUnpackStreamContext
{
    /// <summary>The builder receiving the extracted files.</summary>
    private readonly IBuilder _builder;

    /// <summary>The the archive data to be extracted.</summary>
    private readonly Stream _stream;

    /// <summary>Callback for normalizing the path of archive entries.</summary>
    private readonly Func<string, string> _normalizePath;

    /// <summary>Used to signal when the user wishes to cancel the extraction.</summary>
    private readonly CancellationToken _cancellationToken;

    public Stream OpenArchiveReadStream(int archiveNumber, string archiveName, CompressionEngine compressionEngine)
        => new DuplicateStream(_stream);

    public void CloseArchiveReadStream(int archiveNumber, string archiveName, Stream stream)
    {}

    private Task? _task;
    private Pipe? _pipe;

    public Stream? OpenFileWriteStream(string path, long fileSize, DateTime lastWriteTime)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (_normalizePath(path) is not {} relativePath) return null;

        _pipe = new();

        var readStream = _pipe.Reader.AsStream().WithLength(fileSize);
        _task = Task.Run(() => _builder.AddFile(relativePath, readStream, DateTime.SpecifyKind(lastWriteTime, DateTimeKind.Utc)), _cancellationToken);

        return _pipe.Writer.AsStream();
    }

    public void CloseFileWriteStream(string path, Stream stream, FileAttributes attributes, DateTime lastWriteTime)
    {
        _pipe?.Writer.Complete();
        stream.Dispose();

        _task?.Wait(_cancellationToken);
        _pipe?.Reader.Complete();
    }
}
