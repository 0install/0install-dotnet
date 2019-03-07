// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NanoByte.Common.Net;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// A parameter structure (data transfer object) containing information about a requested archive extraction.
    /// </summary>
    /// <see cref="IImplementationStore.AddArchives"/>
    [Serializable]
    public struct ArchiveFileInfo : IEquatable<ArchiveFileInfo>
    {
        /// <summary>
        /// The file to be extracted.
        /// </summary>
        [NotNull]
        public string Path { get; set; }

        /// <summary>
        /// The name of the subdirectory in the archive to extract (with Unix-style slashes); <c>null</c> to extract entire archive.
        /// </summary>
        [CanBeNull]
        public string Extract { get; set; }

        /// <summary>
        /// The subdirectory within the implementation directory to extract this archive to; <c>null</c> for none.
        /// </summary>
        [CanBeNull]
        public string Destination { get; set; }

        /// <summary>
        /// The MIME type of archive format of the file; <c>null</c> to guess.
        /// </summary>
        [CanBeNull]
        public string MimeType { get; set; }

        /// <summary>
        /// The number of bytes at the beginning of the file which should be ignored.
        /// </summary>
        public long StartOffset { get; set; }

        /// <summary>
        /// The URL the file was originally downloaded from.
        /// </summary>
        /// <remarks>This is used to provide additional information in case of an exception.</remarks>
        [CanBeNull]
        public Uri OriginalSource { get; set; }

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
        public bool Equals(ArchiveFileInfo other)
            => string.Equals(Extract, other.Extract)
            && string.Equals(Destination, other.Destination)
            && string.Equals(MimeType, other.MimeType)
               // NOTE: Exclude Path from comparison to allow easy testing with randomized TemporaryFiles
            && StartOffset == other.StartOffset
            && OriginalSource == other.OriginalSource;

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2231:OverloadOperatorEqualsOnOverridingValueTypeEquals", Justification = "Equals() method is only used for easier unit testing")]
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is ArchiveFileInfo info && Equals(info);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                // NOTE: Exclude Path from comparison to allow easy testing with randomized TemporaryFiles
                int hashCode = Extract?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Destination?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (MimeType?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ StartOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ (OriginalSource?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
        #endregion
    }
}
