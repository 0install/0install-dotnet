// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using NanoByte.Common.Net;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// A parameter object containing information about a requested archive extraction.
    /// </summary>
    /// <see cref="IImplementationStore.AddArchives"/>
    [Serializable]
    public class ArchiveFileInfo : IEquatable<ArchiveFileInfo>
    {
        /// <summary>
        /// The file to be extracted.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The MIME type of archive format of the file.
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// Creates a new archive file info object.
        /// </summary>
        /// <param name="path">The file to be extracted.</param>
        /// <param name="mimeType">The MIME type of archive format of the file.</param>
        public ArchiveFileInfo(string path, string mimeType)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        }

        /// <summary>
        /// The name of the subdirectory in the archive to extract (with Unix-style slashes); <c>null</c> to extract entire archive.
        /// </summary>
        public string? Extract { get; set; }

        /// <summary>
        /// The subdirectory within the implementation directory to extract this archive to; <c>null</c> for none.
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// The number of bytes at the beginning of the file which should be ignored.
        /// </summary>
        public long StartOffset { get; set; }

        /// <summary>
        /// The URL the file was originally downloaded from.
        /// </summary>
        /// <remarks>This is used to provide additional information in case of an exception.</remarks>
        public Uri? OriginalSource { get; set; }

        /// <summary>
        /// Returns the archive in the form "ArchiveFileInfo: Path (MimeType, + StartOffset, SubDir) => Destination". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            string result = $"ArchiveFileInfo: {Path} ({MimeType}, + {StartOffset}, {Extract})";
            if (!string.IsNullOrEmpty(Destination)) result += " => " + Destination;
            if (OriginalSource != null) result += ", originally from: " + OriginalSource.ToStringRfc();
            return result;
        }

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ArchiveFileInfo? other)
            => other != null
            && Extract == other.Extract
            && Destination == other.Destination
            && MimeType == other.MimeType
            // NOTE: Exclude Path from comparison to allow easy testing with randomized TemporaryFiles
            && StartOffset == other.StartOffset
            && OriginalSource == other.OriginalSource;

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2231:OverloadOperatorEqualsOnOverridingValueTypeEquals", Justification = "Equals() method is only used for easier unit testing")]
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            return obj is ArchiveFileInfo info && Equals(info);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                Extract,
                Destination,
                MimeType,
                StartOffset,
                OriginalSource);
        #endregion
    }
}
