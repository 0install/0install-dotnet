using System;
using System.IO;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Provides content for an building implementation by extracting an archive.
    /// </summary>
    /// <param name="Path">The file to be extracted.</param>
    /// <param name="MimeType">The MIME type indicated the archive format of the file.</param>
    /// <see cref="IImplementationStore.Add"/>
    [Serializable]
    public sealed record ArchiveImplementationSource(string Path, string MimeType) : IImplementationSource
    {
        /// <summary>
        /// The directory to extract into relative to the implementation root as a Unix-style path; <c>null</c> or <see cref="string.Empty"/> for entire archive.
        /// </summary>
        public string? Extract { get; init; }

        /// <summary>
        /// The subdirectory below the implementation directory to extract the archive into as a Unix-style path; <c>null</c> or <see cref="string.Empty"/> for top-level.
        /// </summary>
        public string? Destination { get; init; }

        /// <summary>
        /// The number of bytes at the beginning of the file which should be ignored.
        /// </summary>
        public long StartOffset { get; init; }

        /// <summary>
        /// The path or URL the archive was originally acquired from.
        /// </summary>
        public string OriginalSource { get; init; } = Path;

        /// <inheritdoc/>
        public ITask GetApplyTask(string targetPath)
        {
            if (!string.IsNullOrEmpty(Destination) && FileUtils.IsBreakoutPath(Destination))
                throw new IOException(string.Format(Resources.RecipeInvalidPath, Destination));

            var extractor = ArchiveExtractor.Create(Path, targetPath, MimeType, StartOffset);
            extractor.Extract = Extract;
            extractor.TargetSuffix = FileUtils.UnifySlashes(Destination);
            return extractor;
        }
    }
}
