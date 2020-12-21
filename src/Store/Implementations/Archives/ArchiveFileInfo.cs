// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// A parameter object containing information about a requested archive extraction.
    /// </summary>
    /// <see cref="IImplementationStore.AddArchives"/>
    [Serializable]
    public sealed record ArchiveFileInfo(string Path, string MimeType)
    {
        /// <summary>
        /// The name of the subdirectory in the archive to extract (with Unix-style slashes); <c>null</c> to extract entire archive.
        /// </summary>
        public string? Extract { get; init; }

        /// <summary>
        /// The subdirectory within the implementation directory to extract this archive to; <c>null</c> for none.
        /// </summary>
        public string? Destination { get; init; }

        /// <summary>
        /// The number of bytes at the beginning of the file which should be ignored.
        /// </summary>
        public long StartOffset { get; init; }

        /// <summary>
        /// The URL the file was originally downloaded from.
        /// </summary>
        /// <remarks>This is used to provide additional information in case of an exception.</remarks>
        public Uri? OriginalSource { get; init; }
    }
}
