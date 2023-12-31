// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Helper methods for creating <see cref="ConflictData"/> lists.
/// </summary>
public static class ConflictDataUtils
{
    /// <summary>
    /// Checks new <see cref="AccessPoint"/> candidates for conflicts with existing ones.
    /// </summary>
    /// <param name="appList">The <see cref="AppList"/> containing the existing <see cref="AccessPoint"/>s.</param>
    /// <param name="accessPoints">The set of <see cref="AccessPoint"/>s candidates to check.</param>
    /// <param name="appEntry">The <see cref="AppEntry"/> the <paramref name="accessPoints"/> are intended for.</param>
    /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Capability"/> is invalid.</exception>
    /// <exception cref="ConflictException">One or more of the <paramref name="accessPoints"/> would cause a conflict with the existing <see cref="AccessPoint"/>s in <see cref="AppList"/>.</exception>
    public static void CheckForConflicts(this AppList appList, [InstantHandle] IEnumerable<AccessPoint> accessPoints, AppEntry appEntry)
    {
        #region Sanity checks
        if (appList == null) throw new ArgumentNullException(nameof(appList));
        if (accessPoints == null) throw new ArgumentNullException(nameof(accessPoints));
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        var newConflictData = accessPoints.GetConflictData(appEntry);
        var existingConflictData = appList.Entries.GetConflictData();

        foreach ((string conflictId, var newEntry) in newConflictData)
        {
            if (existingConflictData.TryGetValue(conflictId, out var existingEntry))
            {
                // Ignore conflicts that are actually just re-applications of existing access points
                if (existingEntry != newEntry) throw ConflictException.NewConflict(existingEntry, newEntry);
            }
        }
    }

    /// <summary>
    /// Returns all <see cref="ConflictData"/>s for a set of new <see cref="AccessPoint"/> candidates.
    /// </summary>
    /// <param name="accessPoints">The set of <see cref="AccessPoint"/>s candidates to build the list for.</param>
    /// <param name="appEntry">The <see cref="AppEntry"/> the <paramref name="accessPoints"/> are intended for.</param>
    /// <returns>A dictionary of conflict IDs mapping to the according <see cref="ConflictData"/>.</returns>
    /// <exception cref="ConflictException">There are inner conflicts within <paramref name="accessPoints"/>.</exception>
    public static IDictionary<string, ConflictData> GetConflictData(this IEnumerable<AccessPoint> accessPoints, AppEntry appEntry)
    {
        #region Sanity checks
        if (accessPoints == null) throw new ArgumentNullException(nameof(accessPoints));
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        var newConflictIDs = new Dictionary<string, ConflictData>();
        foreach (var accessPoint in accessPoints)
        {
            foreach (string conflictID in accessPoint.GetConflictIDs(appEntry))
            {
                var conflictData = new ConflictData(accessPoint, appEntry);
                if (!newConflictIDs.TryAdd(conflictID, conflictData))
                    throw ConflictException.InnerConflict(conflictData, newConflictIDs[conflictID]);
            }
        }
        return newConflictIDs;
    }

    /// <summary>
    /// Returns all <see cref="ConflictData"/>s for a set of existing <see cref="AppEntry"/>s.
    /// </summary>
    /// <param name="appEntries">The <see cref="AppEntry"/>s to build the list for.</param>
    /// <returns>A dictionary of conflict IDs mapping to the according <see cref="ConflictData"/>.</returns>
    /// <exception cref="ConflictException">There are preexisting conflicts within <paramref name="appEntries"/>.</exception>
    public static IDictionary<string, ConflictData> GetConflictData(this IEnumerable<AppEntry> appEntries)
    {
        #region Sanity checks
        if (appEntries == null) throw new ArgumentNullException(nameof(appEntries));
        #endregion

        var conflictIDs = new Dictionary<string, ConflictData>();
        foreach (var appEntry in appEntries)
        foreach (var accessPoint in appEntry.AccessPoints?.Entries ?? [])
        foreach (string conflictID in accessPoint.GetConflictIDs(appEntry))
        {
            var conflictData = new ConflictData(accessPoint, appEntry);
            if (!conflictIDs.TryAdd(conflictID, conflictData))
                throw ConflictException.ExistingConflict(conflictIDs[conflictID], conflictData);
        }
        return conflictIDs;
    }
}
