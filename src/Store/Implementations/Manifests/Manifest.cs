// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// A manifest lists every file, directory and symlink in the tree and contains a digest of each file's content.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [Serializable]
    public sealed class Manifest : IEquatable<Manifest>, IEnumerable<ManifestNode>
    {
        #region Constants
        /// <summary>
        /// The well-known file name used to store manifest files in directories.
        /// </summary>
        public const string ManifestFile = ".manifest";
        #endregion

        /// <summary>
        /// The format of the manifest (which file details are listed, which digest method is used, etc.).
        /// </summary>
        public ManifestFormat Format { get; }

        private readonly ManifestNode[] _nodes;

        private long _totalSize = -1;

        /// <summary>
        /// The combined size of all files listed in the manifest in bytes.
        /// </summary>
        public long TotalSize
        {
            get
            {
                // Only calculate the total size if it hasn't been cached yet
                if (_totalSize == -1)
                    _totalSize = _nodes.OfType<ManifestFileBase>().Sum(node => node.Size);

                return _totalSize;
            }
        }

        /// <summary>
        /// Creates a new manifest.
        /// </summary>
        /// <param name="format">The format used for <see cref="Save(Stream)"/>, also specifies the algorithm used in <see cref="ManifestDirectoryElement.Digest"/>.</param>
        /// <param name="nodes">A list of all elements in the tree this manifest represents.</param>
        public Manifest(ManifestFormat format, IEnumerable<ManifestNode> nodes)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            _nodes = (nodes ?? throw new ArgumentNullException(nameof(nodes))).ToArray(); // Make the collection immutable
        }

        /// <summary>
        /// Creates a new manifest.
        /// </summary>
        /// <param name="format">The format used for <see cref="Save(Stream)"/>, also specifies the algorithm used in <see cref="ManifestDirectoryElement.Digest"/>.</param>
        /// <param name="nodes">A list of all elements in the tree this manifest represents.</param>
        public Manifest(ManifestFormat format, params ManifestNode[] nodes)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        /// <summary>
        /// Lists the paths of all <see cref="ManifestNode"/>s relative to the manifest root.
        /// </summary>
        /// <returns>A mapping of relative paths to <see cref="ManifestNode"/>s.</returns>
        /// <remarks>This handles the fact that <see cref="ManifestDirectoryElement"/>s inherit their location from the last <see cref="ManifestDirectory"/> that precedes them.</remarks>
        [Pure]
        public IReadOnlyDictionary<string, ManifestNode> ListPaths()
        {
#if NETFRAMEWORK
            var result = new Dictionary<string, ManifestNode>();
#else
            var result = new SortedDictionary<string, ManifestNode>();
#endif

            string dirPath = "";
            foreach (var node in this)
            {
                switch (node)
                {
                    case ManifestDirectory dir:
                        dirPath = FileUtils.UnifySlashes(dir.FullPath).Substring(1);
                        result.Add(dirPath, dir);
                        break;
                    case ManifestDirectoryElement element:
                        string elementPath = Path.Combine(dirPath, element.Name);
                        result.Add(elementPath, element);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a copy of the manifest with all timestamps shifted by the specified <paramref name="offset"/> and rounded up to an even number of seconds.
        /// </summary>
        public Manifest WithOffset(TimeSpan offset)
            => new(Format, _nodes.Select(node => node switch
            {
                ManifestNormalFile normal => new ManifestNormalFile(normal.Digest, Round(normal.ModifiedTime) + offset, normal.Size, normal.Name),
                ManifestExecutableFile executable => new ManifestExecutableFile(executable.Digest, Round(executable.ModifiedTime) + offset, executable.Size, executable.Name),
                _ => node
            }));

        private static DateTime Round(DateTime timestamp)
            => FileUtils.FromUnixTime((timestamp.ToUnixTime() + 1) / 2 * 2);

        #region Storage
        /// <summary>
        /// Writes the manifest to a file and calculates its digest.
        /// </summary>
        /// <param name="path">The path of the file to write.</param>
        /// <returns>The manifest digest.</returns>
        /// <exception cref="IOException">A problem occurred while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
        /// <remarks>
        /// The exact format is specified here: https://docs.0install.net/specifications/manifest/
        /// </remarks>
        public string Save(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            using (var stream = File.Create(path))
                Save(stream);

            // Calculate the digest of the completed manifest file
            using (var stream = File.OpenRead(path))
                return Format.Prefix + Format.Separator + Format.DigestManifest(stream);
        }

        /// <summary>
        /// Writes the manifest to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <returns>The manifest digest.</returns>
        /// <remarks>
        /// The exact format is specified here: https://docs.0install.net/specifications/manifest/
        /// </remarks>
        public void Save(Stream stream)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            // Use UTF-8 without BOM and Unix-style line breaks to ensure correct digest values
            var writer = new StreamWriter(stream, encoding: FeedUtils.Encoding) {NewLine = "\n"};

            // Write one line for each node
            foreach (var node in _nodes)
                writer.WriteLine(node.ToString());

            writer.Flush();
        }

        /// <summary>
        /// Returns the manifest in the same format used by <see cref="Save(Stream)"/>.
        /// </summary>
        public override string ToString()
        {
            var output = new StringBuilder();
            foreach (var node in _nodes)
                output.Append(node + "\n");
            return output.ToString();
        }

        /// <summary>
        /// Parses a manifest file stream.
        /// </summary>
        /// <param name="stream">The stream to load from.</param>
        /// <param name="format">The format of the file and the format of the created <see cref="Manifest"/>. Comprises the digest method used and the file's format.</param>
        /// <returns>A set of <see cref="ManifestNode"/>s containing the parsed content of the file.</returns>
        /// <exception cref="FormatException">The file specified is not a valid manifest file.</exception>
        /// <remarks>
        /// The exact format is specified here: https://docs.0install.net/specifications/manifest/
        /// </remarks>
        public static Manifest Load(Stream stream, ManifestFormat format)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (format == null) throw new ArgumentNullException(nameof(format));
            #endregion

            var nodes = new List<ManifestNode>();

            var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                // Parse each line as a node
                string line = reader.ReadLine() ?? "";
                if (line.StartsWith("F")) nodes.Add(ManifestNormalFile.FromString(line));
                else if (line.StartsWith("X")) nodes.Add(ManifestExecutableFile.FromString(line));
                else if (line.StartsWith("S")) nodes.Add(ManifestSymlink.FromString(line));
                else if (line.StartsWith("D")) nodes.Add(ManifestDirectory.FromString(line));
                else throw new FormatException(Resources.InvalidLinesInManifest);
            }

            return new Manifest(format, nodes);
        }

        /// <summary>
        /// Parses a manifest file.
        /// </summary>
        /// <param name="path">The path of the file to load.</param>
        /// <param name="format">The format of the file and the format of the created <see cref="Manifest"/>. Comprises the digest method used and the file's format.</param>
        /// <returns>A set of <see cref="ManifestNode"/>s containing the parsed content of the file.</returns>
        /// <exception cref="FormatException">The file specified is not a valid manifest file.</exception>
        /// <exception cref="IOException">The manifest file could not be read.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        /// <remarks>
        /// The exact format is specified here: https://docs.0install.net/specifications/manifest/
        /// </remarks>
        public static Manifest Load(string path, ManifestFormat format)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (format == null) throw new ArgumentNullException(nameof(format));
            #endregion

            using var stream = File.OpenRead(path);
            return Load(stream, format);
        }

        /// <summary>
        /// Calculates the digest for the manifest in-memory.
        /// </summary>
        /// <returns>The manifest digest.</returns>
        public string CalculateDigest()
        {
            using var stream = new MemoryStream();
            Save(stream);

            stream.Position = 0;
            return Format.Prefix + Format.Separator + Format.DigestManifest(stream);
        }
        #endregion

        #region Enumeration
        IEnumerator<ManifestNode> IEnumerable<ManifestNode>.GetEnumerator() => ((IEnumerable<ManifestNode>)_nodes).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

        /// <summary>
        /// Retrieves a specific <see cref="ManifestNode"/>.
        /// </summary>
        /// <param name="i">The index of the node to retreive.</param>
        public ManifestNode this[int i] => _nodes[i];
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Manifest? other)
            => other != null
            && Format == other.Format
            && _nodes.SequencedEquals(other._nodes);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Manifest manifest && Equals(manifest);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Format, _nodes.GetSequencedHashCode());
        #endregion
    }
}
