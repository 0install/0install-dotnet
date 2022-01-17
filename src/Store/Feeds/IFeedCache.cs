// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using ZeroInstall.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds;

/// <summary>
/// A disk-based cache of <see cref="Feed"/>s that were downloaded via HTTP(S).
/// Once a feed has been added to this cache it is considered trusted (signatures are not checked again).
/// </summary>
/// <remarks>Implementations of this interface are immutable and thread-safe.</remarks>
public interface IFeedCache
{
    /// <summary>
    /// Determines whether this cache contains a local copy of a <see cref="Feed"/> identified by a specific URL.
    /// </summary>
    /// <param name="feedUri">The canonical ID used to identify the feed.</param>
    /// <returns>
    ///   <c>true</c> if the specified feed is available in this cache;
    ///   <c>false</c> if the specified feed is not available in this cache.
    /// </returns>
    bool Contains(FeedUri feedUri);

    /// <summary>
    /// Returns a list of all <see cref="Feed"/>s stored in this cache.
    /// </summary>
    /// <returns>
    /// A list of feed URIs (e.g. "http://somedomain.net/interface.xml").
    /// Usually these can also be considered interface URIs.
    /// </returns>
    /// <exception cref="IOException">A problem occurred while reading from the cache.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the cache is not permitted.</exception>
    IEnumerable<FeedUri> ListAll();

    /// <summary>
    /// Tries to get a specific <see cref="Feed"/> from this cache.
    /// </summary>
    /// <param name="feedUri">The canonical ID used to identify the feed.</param>
    /// <returns>The un-normalized <see cref="Feed"/>; <c>null</c> if the feed was not found in the cache.</returns>
    /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the cache is not permitted.</exception>
    /// <exception cref="InvalidDataException">The feed file could not be parsed.</exception>
    Feed? GetFeed(FeedUri feedUri);

    /// <summary>
    /// Determines which signatures a <see cref="Feed"/> from this cache is signed with.
    /// </summary>
    /// <param name="feedUri">The canonical ID used to identify the feed.</param>
    /// <returns>A list of signatures found, both valid and invalid. Returns an empty list if the feed was not found in the cache.</returns>
    /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the cache is not permitted.</exception>
    /// <exception cref="SignatureException">There is no valid signature data embedded in the feed data.</exception>
    IEnumerable<OpenPgpSignature> GetSignatures(FeedUri feedUri);

    /// <summary>
    /// Tries to get the file path of the on-disk representation of a specific <see cref="Feed"/>.
    /// </summary>
    /// <param name="feedUri">The canonical ID used to identify the feed.</param>
    /// <returns>The fully qualified path to the feed file; <c>null</c> if the feed was not found in the cache.</returns>
    /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the cache is not permitted.</exception>
    string? GetPath(FeedUri feedUri);

    /// <summary>
    /// Adds a new <see cref="Feed"/> to the cache. Only do this after the feed source has been verified and trusted and replay attacks filtered!
    /// </summary>
    /// <param name="feedUri">The canonical ID used to identify the feed. Must not be a local path.</param>
    /// <param name="data">The content of the feed file as a byte array.</param>
    /// <exception cref="IOException">A problem occurred while writing the feed file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the cache is not permitted.</exception>
    /// <exception cref="InvalidDataException">The feed file could not be parsed.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="feedUri"/> is a a local path.</exception>
    void Add(FeedUri feedUri, byte[] data);

    /// <summary>
    /// Removes a specific <see cref="Feed"/> from this cache. No exception is thrown if the specified <see cref="Feed"/> is not in the cache.
    /// </summary>
    /// <param name="feedUri">The canonical ID used to identify the feed.</param>
    /// <exception cref="IOException">The feed could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the cache is not permitted.</exception>
    void Remove(FeedUri feedUri);
}
