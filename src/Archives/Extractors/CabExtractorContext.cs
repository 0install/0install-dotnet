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
/// <param name="builder">The builder receiving the extracted files.</param>
/// <param name="stream">The the archive data to be extracted.</param>
/// <param name="normalizePath">Callback for normalizing the path of archive entries.</param>
/// <param name="cancellationToken">Used to signal when the user wishes to cancel the extraction.</param>
internal sealed class CabExtractorContext(IBuilder builder, Stream stream, Func<string, string?> normalizePath, CancellationToken cancellationToken) : IUnpackStreamContext
{
    public Stream OpenArchiveReadStream(int archiveNumber, string archiveName, CompressionEngine compressionEngine)
        => new DuplicateStream(stream);

    public void CloseArchiveReadStream(int archiveNumber, string archiveName, Stream stream)
    {}

    private Task? _task;
    private Pipe? _pipe;

    public Stream? OpenFileWriteStream(string path, long fileSize, DateTime lastWriteTime)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (normalizePath(path) is not {} relativePath) return null;

        _pipe = new();

        var readStream = _pipe.Reader.AsStream().WithLength(fileSize);
        _task = Task.Run(() => builder.AddFile(relativePath, readStream, DateTime.SpecifyKind(lastWriteTime, DateTimeKind.Utc)), cancellationToken);

        return _pipe.Writer.AsStream();
    }

    public void CloseFileWriteStream(string path, Stream stream, FileAttributes attributes, DateTime lastWriteTime)
    {
        _pipe?.Writer.Complete();
        stream.Dispose();

        _task?.Wait(cancellationToken);
        _pipe?.Reader.Complete();
    }
}
