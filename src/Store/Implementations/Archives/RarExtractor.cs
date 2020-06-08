// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Archives.Rar;
using SharpCompress.Readers;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a RAR archive.
    /// </summary>
    public class RarExtractor : SharpCompressArchiveExtractor
    {
        /// <summary>
        /// Prepares to extract a RAR archive contained in a stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal RarExtractor(Stream stream, string targetPath)
            : base(RarArchive.Open(stream, new ReaderOptions {LeaveStreamOpen = false}), targetPath)
        {}
    }
}
