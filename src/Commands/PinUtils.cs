// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Commands;

/// <summary>
/// Marks specific implementation versions as preferred for future runs.
/// </summary>
internal static class PinUtils
{
    /// <summary>
    /// Pins a specific implementation for future runs.
    /// </summary>
    public static void Pin(ImplementationSelection implementation)
        => FeedPreferences.UpdateFor(
            implementation.FromFeed ?? implementation.InterfaceUri,
            preferences => preferences[implementation.ID].UserStability = Stability.Preferred);

    /// <summary>
    /// Unpins all previously pinned implementations.
    /// </summary>
    public static void Unpin(FeedUri interfaceUri)
    {
        var additionalFeeds = InterfacePreferences.LoadFor(interfaceUri).Feeds.Select(x => x.Source);
        foreach (var feedUri in additionalFeeds.Prepend(interfaceUri))
        {
            FeedPreferences.UpdateFor(
                feedUri,
                preferences =>
                {
                    foreach (var implementation in preferences.Implementations)
                    {
                        if (implementation.UserStability == Stability.Preferred)
                            implementation.UserStability = Stability.Unset;
                    }
                });
        }
    }
}
