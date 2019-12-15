// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Provides access to a disk-based cache of <see cref="Feed"/>s that were downloaded via HTTP(S).
    /// </summary>
    /// <remarks>
    ///   <para>Local feed files are simply passed through this cache.</para>
    ///   <para>Once a feed has been added to this cache it is considered trusted (signatures are not checked again).</para>
    /// </remarks>
    public sealed class DiskFeedCache : IFeedCache
    {
        #region Dependencies
        private readonly IOpenPgp _openPgp;

        /// <summary>
        /// Creates a new disk-based cache.
        /// </summary>
        /// <param name="directoryPath">A fully qualified directory path.</param>
        /// <param name="openPgp">Provides access to an encryption/signature system compatible with the OpenPGP standard.</param>
        public DiskFeedCache([NotNull] string directoryPath, [NotNull] IOpenPgp openPgp)
        {
            DirectoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
            _openPgp = openPgp ?? throw new ArgumentNullException(nameof(openPgp));
        }
        #endregion

        /// <summary>
        /// The directory containing the cached <see cref="Feed"/>s.
        /// </summary>
        [NotNull]
        public string DirectoryPath { get; }

        /// <inheritdoc/>
        public bool Contains(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            // Local files are passed through directly
            if (feedUri.IsFile) return File.Exists(feedUri.LocalPath);

            return FileUtils.ExistsCaseSensitive(Path.Combine(DirectoryPath, feedUri.Escape()));
        }

        /// <inheritdoc/>
        public IEnumerable<FeedUri> ListAll()
        {
            if (!Directory.Exists(DirectoryPath)) return Enumerable.Empty<FeedUri>();

            // ReSharper disable once AssignNullToNotNullAttribute
            return Directory.GetFiles(DirectoryPath)
                            .TrySelect<string, FeedUri, UriFormatException>(x => FeedUri.Unescape(Path.GetFileName(x)));
        }

        /// <inheritdoc/>
        public Feed GetFeed(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            string path = GetPath(feedUri);
            Log.Debug("Loading feed " + feedUri.ToStringRfc() + " from disk cache: " + path);

            return XmlStorage.LoadXml<Feed>(path);
        }

        /// <inheritdoc/>
        public IEnumerable<OpenPgpSignature> GetSignatures(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            return FeedUtils.GetSignatures(_openPgp, File.ReadAllBytes(GetPath(feedUri)));
        }

        /// <inheritdoc/>
        public string GetPath(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            if (feedUri.IsFile) throw new KeyNotFoundException("Feed cache does not handle local files: " + feedUri.ToStringRfc());

            string fileName = feedUri.Escape();
            string path = Path.Combine(DirectoryPath, fileName);
            if (FileUtils.ExistsCaseSensitive(path)) return path;
            else throw new KeyNotFoundException(string.Format(Resources.FeedNotInCache, feedUri, path));
        }

        /// <inheritdoc/>
        public void Add(FeedUri feedUri, byte[] data)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            if (data == null) throw new ArgumentNullException(nameof(data));
            #endregion

            if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);

            try
            {
                string path = Path.Combine(DirectoryPath, feedUri.Escape());
                Log.Debug("Adding feed " + feedUri.ToStringRfc() + " to disk cache: " + path);
                WriteToFile(data, path);
            }
            catch (PathTooLongException)
            {
                Log.Info("File path in feed cache too long. Using hash of feed URI to shorten path.");
                WriteToFile(data, Path.Combine(DirectoryPath, feedUri.AbsoluteUri.Hash(SHA256.Create())));
            }
        }

        /// <summary>
        /// Writes the entire content of a byte array to file atomically.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="path">The file to write to.</param>
        private static void WriteToFile(byte[] data, string path)
        {
            using var atomic = new AtomicWrite(path);
            File.WriteAllBytes(atomic.WritePath, data);
            atomic.Commit();
        }

        /// <inheritdoc/>
        public void Remove(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
            #endregion

            string path = GetPath(feedUri);
            Log.Debug("Removing feed " + feedUri.ToStringRfc() + " from disk cache: " + path);
            File.Delete(path);
        }

        #region Conversion
        /// <summary>
        /// Returns the store in the form "DiskFeedCache: DirectoryPath". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"DiskFeedCache: {DirectoryPath}";
        #endregion
    }
}
