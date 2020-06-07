// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a 7z archive.
    /// </summary>
    public class SevenZipExtractor : SharpCompressArchiveExtractor
    {
        /// <summary>
        /// Prepares to extract a 7z archive contained in a stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal SevenZipExtractor(Stream stream, string targetPath)
            : base(SevenZipArchive.Open(stream, new ReaderOptions {LeaveStreamOpen = false}), targetPath)
        {}
    }
}
