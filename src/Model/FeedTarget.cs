// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Associates a <see cref="FeedUri"/> with the <see cref="Model.Feed"/> data acquired from there.
/// </summary>
/// <param name="Uri">The URI or local path (must be absolute) to the feed.</param>
/// <param name="Feed">The data acquired from <paramref name="Uri"/>. <see cref="Model.Feed.Normalize"/> has already been called.</param>
public record FeedTarget(FeedUri Uri, Feed Feed)
{
    public override string ToString() => Uri.ToStringRfc();
}
