// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish;

/// <summary>
/// Creates new <see cref="Feed"/>s from scratch or from local feeds.
/// </summary>
public static class FeedTemplate
{
    /// <summary>
    /// Creates a new feed pre-filled with placeholder values.
    /// </summary>
    /// <param name="name">The name of the application the feed is for.</param>
    public static Feed Create([Localizable(false)] string name = "My App")
        => new()
        {
            Name = name,
            Summaries = {"cures all ills"},
            Descriptions =
            {
                """
                A longer, multi-line description of the behaviour of the program goes here. State clearly what the program is for (clearly enough that people who don't want it will realise too).

                Use a blank line to separate paragraphs.
                """
            },
            Elements =
            {
                new Group
                {
                    Main = "myprog",
                    Elements = {new Implementation {ID = ".", Version = new("0.1")}}
                }
            }
        };

    /// <summary>
    /// Creates a master feed from a local feed, turning its <see cref="Feed.FeedFor"/> reference into the feed's own <see cref="Feed.Uri"/>.
    /// </summary>
    /// <param name="localFeed">The local feed to convert. Not modified.</param>
    /// <returns>The new master feed.</returns>
    /// <exception cref="InvalidDataException"><paramref name="localFeed"/> does not have exactly one <see cref="Feed.FeedFor"/> reference.</exception>
    public static Feed CreateFromLocal(Feed localFeed)
    {
        #region Sanity checks
        if (localFeed == null) throw new ArgumentNullException(nameof(localFeed));
        #endregion

        var uri = localFeed.FeedFor switch
        {
            [] => throw new InvalidDataException(string.Format(Resources.NoFeedFor, "<feed-for>")),
            [{Target: {} target}] => target,
            [_] => throw new InvalidDataException(string.Format(Resources.NoFeedFor, "<feed-for>")),
            _ => throw new InvalidDataException(string.Format(Resources.MultipleFeedFor, "<feed-for>"))
        };

        var feed = localFeed.Clone();
        feed.Uri = uri;
        feed.FeedFor.Clear();
        return feed;
    }
}
