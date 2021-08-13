// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using System.IO;
using System.Threading;
using Microsoft.Deployment.Compression;
using NanoByte.Common.Streams;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Used to hold state while extracting a MS Cabinet (.cab).
    /// </summary>
    /// <param name="Builder">The builder receiving the extracted files.</param>
    /// <param name="Stream">The the archive data to be extracted.</param>
    /// <param name="NormalizePath">Callback for normalizing the path of archive entries.</param>
    /// <param name="CancellationToken">Used to signal when the user wishes to cancel the extraction.</param>
    internal sealed record CabExtractorContext(IBuilder Builder, Stream Stream, Func<string, string> NormalizePath, CancellationToken CancellationToken)
        : IUnpackStreamContext
    {
        public Stream OpenArchiveReadStream(int archiveNumber, string archiveName, CompressionEngine compressionEngine)
            => new DuplicateStream(Stream);

        public void CloseArchiveReadStream(int archiveNumber, string archiveName, Stream stream)
        {}

        private Thread? _thread;

        public Stream? OpenFileWriteStream(string path, long fileSize, DateTime lastWriteTime)
        {
            CancellationToken.ThrowIfCancellationRequested();

            string relativePath = NormalizePath(path);
            if (relativePath == null) return null;

            var stream = new ProducerConsumerStream();
            stream.SetLength(fileSize);
            _thread = new Thread(() => Builder.AddFile(relativePath, stream, DateTime.SpecifyKind(lastWriteTime, DateTimeKind.Utc))) {IsBackground = true};
            _thread.Start();
            return stream;
        }

        public void CloseFileWriteStream(string path, Stream stream, FileAttributes attributes, DateTime lastWriteTime)
        {
            ((ProducerConsumerStream)stream).DoneWriting();
            _thread?.Join();
            stream.Dispose();
        }
    }
}
#endif
