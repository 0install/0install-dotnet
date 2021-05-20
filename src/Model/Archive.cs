// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;
using ZeroInstall.Model.Design;
using ZeroInstall.Model.Properties;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Retrieves an implementation by downloading and extracting an archive.
    /// </summary>
    [Description("Retrieves an implementation by downloading and extracting an archive.")]
    [Serializable, XmlRoot("archive", Namespace = Feed.XmlNamespace), XmlType("archive", Namespace = Feed.XmlNamespace)]
    public sealed class Archive : DownloadRetrievalMethod, IEquatable<Archive>
    {
        #region Constants
        /// <summary>
        /// A <see cref="MimeType"/> value for archives.
        /// </summary>
        public const string
            MimeTypeZip = "application/zip",
            MimeTypeTar = "application/x-tar",
            MimeTypeTarGzip = "application/x-compressed-tar",
            MimeTypeTarBzip = "application/x-bzip-compressed-tar",
            MimeTypeTarLzma = "application/x-lzma-compressed-tar",
            MimeTypeTarLzip = "application/x-lzip-compressed-tar",
            MimeTypeTarXz = "application/x-xz-compressed-tar",
            MimeTypeTarZstandard = "application/x-zstd-compressed-tar",
            MimeTypeRubyGem = "application/x-ruby-gem",
            MimeType7Z = "application/x-7z-compressed",
            MimeTypeRar = "application/vnd.rar",
            MimeTypeCab = "application/vnd.ms-cab-compressed",
            MimeTypeMsi = "application/x-msi",
            MimeTypeDeb = "application/x-deb",
            MimeTypeRpm = "application/x-rpm",
            MimeTypeDmg = "application/x-apple-diskimage";

        /// <summary>
        /// All known <see cref="MimeType"/> values for archives.
        /// </summary>
        public static readonly IEnumerable<string> KnownMimeTypes = new[] {MimeTypeZip, MimeTypeTar, MimeTypeTarGzip, MimeTypeTarBzip, MimeTypeTarLzma, MimeTypeTarLzip, MimeTypeTarXz, MimeTypeTarZstandard, MimeTypeRubyGem, MimeType7Z, MimeTypeRar, MimeTypeCab, MimeTypeMsi, MimeTypeDeb, MimeTypeRpm, MimeTypeDmg};

        /// <summary>
        /// Tries to guess the MIME type of an archive file by looking at its file extension.
        /// </summary>
        /// <param name="fileName">The file name to analyze.</param>
        /// <exception cref="NotSupportedException">The file extension does not correspond to a known archive type.</exception>
        public static string GuessMimeType(string fileName)
        {
            #region Sanity checks
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            #endregion

            if (fileName.EndsWithIgnoreCase(".zip")) return MimeTypeZip;
            if (fileName.EndsWithIgnoreCase(".tar")) return MimeTypeTar;
            if (fileName.EndsWithIgnoreCase(".tar.gz") || fileName.EndsWithIgnoreCase(".tgz")) return MimeTypeTarGzip;
            if (fileName.EndsWithIgnoreCase(".tar.bz2") || fileName.EndsWithIgnoreCase(".tbz2") || fileName.EndsWithIgnoreCase(".tbz")) return MimeTypeTarBzip;
            if (fileName.EndsWithIgnoreCase(".tar.lzma") || fileName.EndsWithIgnoreCase(".tlzma")) return MimeTypeTarLzma;
            if (fileName.EndsWithIgnoreCase(".tar.lz") || fileName.EndsWithIgnoreCase(".tlz")) return MimeTypeTarLzma;
            if (fileName.EndsWithIgnoreCase(".tar.xz") || fileName.EndsWithIgnoreCase(".txz")) return MimeTypeTarXz;
            if (fileName.EndsWithIgnoreCase(".tar.zst")) return MimeTypeTarZstandard;
            if (fileName.EndsWithIgnoreCase(".gem")) return MimeTypeRubyGem;
            if (fileName.EndsWithIgnoreCase(".7z")) return MimeType7Z;
            if (fileName.EndsWithIgnoreCase(".rar")) return MimeTypeRar;
            if (fileName.EndsWithIgnoreCase(".cab")) return MimeTypeCab;
            if (fileName.EndsWithIgnoreCase(".msi")) return MimeTypeMsi;
            if (fileName.EndsWithIgnoreCase(".deb")) return MimeTypeDeb;
            if (fileName.EndsWithIgnoreCase(".rpm")) return MimeTypeRpm;
            if (fileName.EndsWithIgnoreCase(".dmg")) return MimeTypeDmg;
            throw new NotSupportedException(string.Format(Resources.UnknownArchiveType, Path.GetExtension(fileName)));
        }

        /// <summary>
        /// Gets the default file extension for a particular archive MIME type.
        /// </summary>
        /// <param name="mimeType">The MIME type to get the extension for.</param>
        /// <returns>The file extension including the leading dot, e.g. '.zip'.</returns>
        /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> is not in the list of <see cref="KnownMimeTypes"/>.</exception>
        public static string GetDefaultExtension(string mimeType)
            => (mimeType ?? throw new ArgumentNullException(nameof(mimeType))) switch
            {
                MimeTypeZip => ".zip",
                MimeTypeTar => ".tar",
                MimeTypeTarGzip => ".tar.gz",
                MimeTypeTarBzip => ".tar.bz2",
                MimeTypeTarLzma => ".tar.lzma",
                MimeTypeTarLzip => ".tar.lzip",
                MimeTypeRubyGem => ".gem",
                MimeType7Z => ".7z",
                MimeTypeRar => ".rar",
                MimeTypeCab => ".cab",
                MimeTypeMsi => ".msi",
                _ => throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType))
            };
        #endregion

        /// <summary>
        /// The type of the archive as a MIME type. If missing, the type is guessed from the extension on the <see cref="DownloadRetrievalMethod.Href"/> attribute. This value is case-insensitive.
        /// </summary>
        [Description("The type of the archive as a MIME type. If missing, the type is guessed from the extension on the location attribute. This value is case-insensitive.")]
        [TypeConverter(typeof(ArchiveMimeTypeConverter))]
        [XmlAttribute("type"), DefaultValue("")]
        public string? MimeType { get; set; }

        /// <summary>
        /// The number of bytes at the beginning of the file which should be ignored. The value in <see cref="DownloadRetrievalMethod.Size"/> does not include the skipped bytes.
        /// </summary>
        /// <remarks>This is useful for some self-extracting archives which are made up of a shell script followed by a normal archive in a single file.</remarks>
        [Description("The number of bytes at the beginning of the file which should be ignored. The value in the size attribute does not include the skipped bytes.")]
        [XmlAttribute("start-offset"), DefaultValue(0L)]
        public long StartOffset { get; set; }

        /// <inheritdoc/>
        public override long DownloadSize => Size + StartOffset;

        /// <summary>
        /// The directory to extract into relative to the implementation root as a Unix-style path; <c>null</c> or <see cref="string.Empty"/> for entire archive.
        /// </summary>
        [Description("The directory to extract into relative to the implementation root as a Unix-style path; unset or empty for entire archive.")]
        [XmlAttribute("extract"), DefaultValue("")]
        public string? Extract { get; set; }

        /// <summary>
        /// The subdirectory below the implementation directory to extract the archive into as a Unix-style path; <c>null</c> or <see cref="string.Empty"/> for top-level.
        /// </summary>
        [Description("The subdirectory below the implementation directory to extract the archive into as a Unix-style path; unset or empty for top-level.")]
        [XmlAttribute("dest")]
        public string? Destination { get; set; }

        #region Normalize
        /// <inheritdoc/>
        public override void Normalize(FeedUri? feedUri = null)
        {
            base.Normalize(feedUri);

            // If the MIME type is already set or the location is missing, we have nothing to do here
            if (!string.IsNullOrEmpty(MimeType) || Href == null) return;

            // Guess the MIME type based on the file extension
            MimeType = GuessMimeType(Href.OriginalString);
        }

        protected override string XmlTagName => "archive";
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the archive in the form "Location (MimeType, Size + StartOffset, Extract) => Destination". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            string result = $"{Href} ({MimeType}, {Size} + {StartOffset}, {Extract})";
            if (!string.IsNullOrEmpty(Destination)) result += " => " + Destination;
            return result;
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Archive"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Archive"/>.</returns>
        public override RetrievalMethod Clone() => new Archive {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Href = Href, Size = Size, MimeType = MimeType, StartOffset = StartOffset, Extract = Extract, Destination = Destination};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Archive? other)
            => other != null
            && base.Equals(other)
            && StringUtils.EqualsIgnoreCase(other.MimeType, MimeType)
            && other.StartOffset == StartOffset
            && other.Extract == Extract
            && other.Destination == Destination;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Archive archive && Equals(archive);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            if (MimeType != null) hash.Add(MimeType, StringComparer.OrdinalIgnoreCase);
            hash.Add(StartOffset);
            hash.Add(Extract);
            hash.Add(Destination);
            return hash.ToHashCode();
        }
        #endregion
    }
}
