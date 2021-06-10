// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// A manifest lists every directory, file and symlink in a directory and contains a digest of each file's content.
    /// </summary>
    /// <remarks>
    /// See also: https://docs.0install.net/specifications/manifest/
    /// </remarks>
    [Serializable]
    public sealed class Manifest : IReadOnlyDictionary<string, IDictionary<string, ManifestElement>>
    {
        /// <summary>
        /// The well-known file name used to store manifest files in directories.
        /// </summary>
        public const string ManifestFile = ".manifest";

        /// <summary>
        /// The format of the manifest (which file details are listed, which digest method is used, etc.).
        /// </summary>
        public ManifestFormat Format { get; }

        private readonly SortedDictionary<string, SortedDictionary<string, ManifestElement>> _directories = new(StringComparer.Ordinal);

        /// <summary>
        /// The combined size of all files listed in the manifest in bytes.
        /// </summary>
        public long TotalSize
            => _directories.Values
                           .SelectMany(x => x.Values)
                           .Sum(x => x.Size);

        /// <summary>
        /// Creates a new manifest.
        /// </summary>
        /// <param name="format">The format used for <see cref="Save(Stream)"/>, also specifies the algorithm used in <see cref="ManifestElement.Digest"/>.</param>
        public Manifest(ManifestFormat format)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            Add("");
        }

        #region IReadOnlyDictionary
        /// <summary>
        /// Gets or adds directory in the manifest.
        /// </summary>
        /// <param name="key">The Unix path of the directory relative to the implementation root. <see cref="string.Empty"/> for root.</param>
        public IDictionary<string, ManifestElement> this[string key]
            => Add(key);

        /// <inheritdoc/>
        public bool TryGetValue(string key, [NotNullWhen(true)] out IDictionary<string, ManifestElement>? value)
        {
            if (_directories.TryGetValue(key, out var dictionary))
            {
                value = dictionary;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<string, IDictionary<string, ManifestElement>>> IEnumerable<KeyValuePair<string, IDictionary<string, ManifestElement>>>.GetEnumerator()
        {
            foreach ((string directoryPath, var directory) in _directories)
                yield return new(directoryPath, directory);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_directories).GetEnumerator();

        /// <inheritdoc/>
        public int Count => _directories.Count;

        /// <inheritdoc/>
        public bool ContainsKey(string key) => _directories.ContainsKey(key);

        /// <inheritdoc/>
        public IEnumerable<string> Keys => _directories.Keys;

        /// <inheritdoc/>
        public IEnumerable<IDictionary<string, ManifestElement>> Values => _directories.Values;
        #endregion

        /// <summary>
        /// Returns an existing directory or adds a new directory to the manifest.
        /// </summary>
        /// <param name="key">The Unix path of the directory relative to the implementation root.</param>
        /// <returns>A dictionary of elements inside the directory.</returns>
        public SortedDictionary<string, ManifestElement> Add(string key)
            => _directories.GetOrAdd(key, () => new(StringComparer.Ordinal));

        /// <summary>
        /// Removes a directory and all its subdirectories from the manifest.
        /// </summary>
        /// <param name="key">The Unix path of the directory relative to the implementation root.</param>
        /// <returns><c>true</c> if the directory is successfully found and removed; <c>false</c> otherwise. </returns>
        public bool RemoveRecursive(string key)
            => _directories.RemoveAll(x => x.Key == key || x.Key.StartsWith(key + '/'));

        /// <summary>
        /// Moves a directory and all its subdirectories to a new path.
        /// </summary>
        /// <param name="key">The Unix path of the directory relative to the implementation root.</param>
        /// <param name="newKey">The new Unix path of the directory relative to the implementation root.</param>
        /// <returns><c>true</c> if the directory is successfully found and renamed; <c>false</c> otherwise. </returns>
        public bool RenameRecursive(string key, string newKey)
        {
            bool found = false;

            foreach ((string path, var dir) in _directories.ToList())
            {
                void RenameTo(string newPath)
                {
                    Add(newPath).AddRange(dir);
                    _directories.Remove(path);
                    found = true;
                }

                if (path == key)
                    RenameTo(newKey);
                else if (path.StartsWith(key + '/', out string? rest))
                    RenameTo(newKey + '/' + rest);
            }

            return found;
        }

        /// <summary>
        /// Creates a copy of the manifest with all timestamps shifted by the specified <paramref name="offset"/> and rounded up to an even number of seconds.
        /// </summary>
        public Manifest WithOffset(TimeSpan offset)
        {
            var newManifest = new Manifest(Format);

            long Offset(long timestamp)
                => (timestamp + 1) / 2 * 2 + (long)offset.TotalSeconds;

            foreach ((string directoryPath, var directory) in _directories)
            {
                var newDirectory = newManifest[directoryPath];
                foreach ((string elementName, var element) in directory)
                {
                    newDirectory.Add(elementName, element switch
                    {
                        ManifestNormalFile file => file with {ModifiedTime = Offset(file.ModifiedTime)},
                        ManifestExecutableFile file => file with {ModifiedTime = Offset(file.ModifiedTime)},
                        _ => element
                    });
                }
            }

            return newManifest;
        }

        /// <summary>
        /// Writes the manifest to a file and calculates its digest.
        /// </summary>
        /// <param name="path">The path of the file to write.</param>
        /// <returns>The manifest digest.</returns>
        /// <exception cref="IOException">A problem occurred while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
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
        public void Save(Stream stream)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            // Use UTF-8 without BOM and Unix-style line breaks to ensure correct digest values
            var writer = new StreamWriter(stream, encoding: FeedUtils.Encoding) {NewLine = "\n"};

            // Write one line for each node
            foreach ((string directoryPath, var directory) in _directories)
            {
                if (directoryPath != "")
                    writer.WriteLine("D /" + directoryPath);
                foreach ((string elementName, var element) in directory)
                {
                    writer.WriteLine(element switch
                    {
                        ManifestNormalFile(var digest, var ModifiedTime, var size) => "F " + digest + " " + ModifiedTime + " " + size + " " + elementName,
                        ManifestExecutableFile(var digest, var ModifiedTime, var size) => "X " + digest + " " + ModifiedTime + " " + size + " " + elementName,
                        ManifestSymlink(var digest, var size) => "S " + digest + " " + size + " " + elementName,
                        _ => throw new NotSupportedException($"Unknown {nameof(ManifestElement)} type {element.GetType()}.")
                    });
                }
            }

            writer.Flush();
        }

        /// <summary>
        /// Returns the manifest in the same format used by <see cref="Save(Stream)"/>.
        /// </summary>
        public override string ToString()
        {
            using var stream = new MemoryStream();
            Save(stream);
            return stream.ReadToString();
        }

        /// <summary>
        /// Parses a manifest file stream.
        /// </summary>
        /// <param name="stream">The stream to load from.</param>
        /// <param name="format">The format of the file and the format of the created <see cref="Manifest"/>. Comprises the digest method used and the file's format.</param>
        /// <returns>The parsed content of the file.</returns>
        /// <exception cref="FormatException">The file specified is not a valid manifest file.</exception>
        public static Manifest Load(Stream stream, ManifestFormat format)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (format == null) throw new ArgumentNullException(nameof(format));
            #endregion

            var manifest = new Manifest(format);
            var currentDirectory = manifest[""];
            var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                try
                {
                    string line = reader.ReadLine() ?? "";
                    if (line.StartsWith("D /", out string? dirPath))
                        currentDirectory = manifest[dirPath];
                    else if (line.StartsWith("F "))
                    {
                        string[] parts = line.Split(new[] {' '}, 5);
                        if (parts.Length < 5) throw new FormatException(Resources.InvalidNumberOfLineParts);
                        currentDirectory.Add(parts[4], new ManifestNormalFile(parts[1], long.Parse(parts[2]), long.Parse(parts[3])));
                    }
                    else if (line.StartsWith("X "))
                    {
                        string[] parts = line.Split(new[] {' '}, 5);
                        if (parts.Length < 5) throw new FormatException(Resources.InvalidNumberOfLineParts);
                        currentDirectory.Add(parts[4], new ManifestExecutableFile(parts[1], long.Parse(parts[2]), long.Parse(parts[3])));
                    }
                    else if (line.StartsWith("S "))
                    {
                        string[] parts = line.Split(new[] {' '}, 4);
                        if (parts.Length < 4) throw new FormatException(Resources.InvalidNumberOfLineParts);
                        currentDirectory.Add(parts[3], new ManifestSymlink(parts[1], long.Parse(parts[2])));
                    }
                    else throw new FormatException(Resources.InvalidLinesInManifest);
                }
                #region Error handling
                catch (OverflowException ex)
                {
                    throw new FormatException(Resources.NumberTooLarge, ex);
                }
                #endregion
            }

            return manifest;
        }

        /// <summary>
        /// Parses a manifest file.
        /// </summary>
        /// <param name="path">The path of the file to load.</param>
        /// <param name="format">The format of the file and the format of the created <see cref="Manifest"/>. Comprises the digest method used and the file's format.</param>
        /// <returns>The parsed content of the file.</returns>
        /// <exception cref="FormatException">The file specified is not a valid manifest file.</exception>
        /// <exception cref="IOException">The manifest file could not be read.</exception>
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
    }
}
