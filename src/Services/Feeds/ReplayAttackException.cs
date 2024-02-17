// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Indicates a feed file that downloaded by the <see cref="IFeedManager"/> is older than a version already located in the <see cref="IFeedCache"/>.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public sealed class ReplayAttackException : IOException
{
    /// <summary>
    /// The URL of the feed file to be added to the cache.
    /// </summary>
    public Uri FeedUrl { get; }

    /// <summary>
    /// The last changed time stamp of the existing file in the cache.
    /// </summary>
    public DateTime OldTime { get; }

    /// <summary>
    /// The last changed time stamp of the new file to be added.
    /// </summary>
    public DateTime NewTime { get; }

    /// <summary>
    /// Creates a new replay attack exception.
    /// </summary>
    /// <param name="feedUrl">The URL of the feed file to be added to the cache.</param>
    /// <param name="oldTime">The last changed time stamp of the existing file in the cache.</param>
    /// <param name="newTime">The last changed time stamp of the new file to be added.</param>
    public ReplayAttackException(Uri feedUrl, DateTime oldTime, DateTime newTime)
        : base(string.Format(Resources.ReplayAttack, feedUrl, oldTime, newTime))
    {
        FeedUrl = feedUrl;
        OldTime = oldTime;
        NewTime = newTime;
    }

    #region Serialization
#if !NET8_0_OR_GREATER
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private ReplayAttackException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        #region Sanity checks
        if (info == null) throw new ArgumentNullException(nameof(info));
        #endregion

        FeedUrl = new(info.GetString("FeedUrl")!);
        OldTime = info.GetDateTime("OldTime");
        NewTime = info.GetDateTime("NewTime");
    }

    /// <inheritdoc/>
#if NETFRAMEWORK
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        #region Sanity checks
        if (info == null) throw new ArgumentNullException(nameof(info));
        #endregion

        info.AddValue("FeedUrl", FeedUrl.OriginalString);
        info.AddValue("OldTime", OldTime);
        info.AddValue("NewTime", NewTime);

        base.GetObjectData(info, context);
    }
#endif
    #endregion
}

