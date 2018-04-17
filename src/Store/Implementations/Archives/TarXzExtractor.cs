// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NETSTANDARD2_0
using System;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using XZ.NET;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a XZ-compressed TAR archive.
    /// </summary>
    public class TarXzExtractor : TarExtractor
    {
        private readonly Stream _stream;

        /// <summary>
        /// Prepares to extract a TAR archive contained in a XZ-compressed stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal TarXzExtractor([NotNull] Stream stream, [NotNull] string targetPath)
            : base(GetDecompressionStream(stream), targetPath)
        {
            if (!WindowsUtils.IsWindows) throw new NotSupportedException(Resources.ExtractionOnlyOnWindows);

            _stream = stream;
            UnitsTotal = stream.Length;
        }

        /// <summary>
        /// Adds a XZ-extraction layer around a stream.
        /// </summary>
        /// <param name="stream">The stream containing the XZ-compressed data.</param>
        /// <returns>A stream representing the uncompressed data.</returns>
        /// <exception cref="IOException">The compressed stream contains invalid data.</exception>
        internal static Stream GetDecompressionStream(Stream stream)
        {
            try
            {
                return new DisposeWarpperStream(new XZInputStream(stream), stream.Dispose);
            }
            #region Error handling
            catch (Exception ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        /// <inheritdoc/>
        protected override void UpdateProgress()
            => UnitsProcessed = _stream.Position; // Use original stream instead of decompressed stream to track progress
    }
}
#endif
