// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Security.Cryptography;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds;

/// <summary>
/// A disk-based cache of <see cref="Feed"/>s that were downloaded via HTTP(S).
/// Once a feed has been added to this cache it is considered trusted (signatures are not checked again).
/// </summary>
/// <param name="path">A fully qualified directory path.</param>
/// <param name="openPgp">Provides access to an encryption/signature system compatible with the OpenPGP standard.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public sealed class FeedCache(string path, IOpenPgp openPgp) : IFeedCache
{
    /// <inheritdoc/>
    public string Path { get; } = path;

    /// <inheritdoc/>
    public bool Contains(FeedUri feedUri)
    {
        #region Sanity checks
        if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
        #endregion

        return feedUri.IsFile
            ? File.Exists(feedUri.LocalPath)
            : FileUtils.ExistsCaseSensitive(System.IO.Path.Combine(Path, feedUri.Escape()));
    }

    /// <inheritdoc/>
    public IEnumerable<FeedUri> ListAll()
    {
        if (!Directory.Exists(Path)) return [];

        // ReSharper disable once AssignNullToNotNullAttribute
        return Directory.GetFiles(Path)
                        .TrySelect(
                             x => FeedUri.Unescape(System.IO.Path.GetFileName(x)),
                             (UriFormatException _) => {});
    }

    /// <inheritdoc/>
    public Feed? GetFeed(FeedUri feedUri)
    {
        #region Sanity checks
        if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
        #endregion

        if (GetPath(feedUri) is {} path)
        {
            Log.Debug($"Loading feed {feedUri.ToStringRfc()} from disk cache: {path}");
            try
            {
                return XmlStorage.LoadXml<Feed>(path);
            }
            catch (InvalidDataException ex)
            {
                Log.Warn($"Cached copy of feed {feedUri.ToStringRfc()} is corrupt: {path}", ex);
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<OpenPgpSignature> GetSignatures(FeedUri feedUri)
        => GetPath(feedUri ?? throw new ArgumentNullException(nameof(feedUri))) is {} path
            ? FeedUtils.GetSignatures(openPgp, ReadFromFile(path))
            : [];

    /// <inheritdoc/>
    public string? GetPath(FeedUri feedUri)
    {
        #region Sanity checks
        if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
        #endregion

        if (feedUri.IsFile) return feedUri.LocalPath;

        string path = System.IO.Path.Combine(Path, feedUri.Escape());
        return FileUtils.ExistsCaseSensitive(path) ? path : null;
    }

    /// <inheritdoc/>
    public void Add(FeedUri feedUri, byte[] data)
    {
        #region Sanity checks
        if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
        if (data == null) throw new ArgumentNullException(nameof(data));
        #endregion

        if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);

        try
        {
            string path = System.IO.Path.Combine(Path, feedUri.Escape());
            Log.Debug($"Adding feed {feedUri.ToStringRfc()} to disk cache: {path}");
            WriteToFile(data, path);
        }
        catch (PathTooLongException)
        {
            Log.Info("File path in feed cache too long. Using hash of feed URI to shorten path.");
            WriteToFile(data, System.IO.Path.Combine(Path, feedUri.AbsoluteUri.Hash(SHA256.Create())));
        }
    }

    /// <summary>
    /// Reads the content of a file to a byte array.
    /// </summary>
    /// <param name="path">The file to read from.</param>
    private static byte[] ReadFromFile(string path)
    {
        using (new AtomicRead(path))
            return File.ReadAllBytes(path);
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

        if (GetPath(feedUri) is {} path)
        {
            Log.Debug($"Removing feed {feedUri.ToStringRfc()} from disk cache: {path}");
            File.Delete(path);
        }
    }
}
