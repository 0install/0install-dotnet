// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using System.IO;
using Microsoft.Deployment.Compression;
using Microsoft.Deployment.Compression.Cab;
using NanoByte.Common.Native;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Common base class for extractors for Microsoft archive formats.
    /// </summary>
    public abstract class MicrosoftExtractor : ArchiveExtractor, IUnpackStreamContext
    {
        protected readonly CabEngine CabEngine = new();
        protected Stream? CabStream;

        protected MicrosoftExtractor(string targetPath)
            : base(targetPath)
        {
            if (!WindowsUtils.IsWindows) throw new NotSupportedException(Resources.ExtractionOnlyOnWindows);
        }

        Stream IUnpackStreamContext.OpenArchiveReadStream(int archiveNumber, string archiveName, CompressionEngine compressionEngine) => new DuplicateStream(CabStream);

        void IUnpackStreamContext.CloseArchiveReadStream(int archiveNumber, string archiveName, Stream stream) {}

        private long _bytesStaged;

        Stream? IUnpackStreamContext.OpenFileWriteStream(string path, long fileSize, DateTime lastWriteTime)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            CancellationToken.ThrowIfCancellationRequested();

            string relativePath = GetRelativePath(path);
            if (relativePath == null) return null;

            _bytesStaged = fileSize;

            string absolutePath = DirectoryBuilder.NewFilePath(relativePath, lastWriteTime);
            return File.Create(absolutePath);
        }

        void IUnpackStreamContext.CloseFileWriteStream(string path, Stream stream, FileAttributes attributes, DateTime lastWriteTime)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            stream.Dispose();
            UnitsProcessed += _bytesStaged;
        }

        public override void Dispose()
        {
            CabStream?.Dispose();
            CabEngine.Dispose();
        }
    }
}
#endif
