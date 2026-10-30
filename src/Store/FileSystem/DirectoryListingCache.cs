// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Looks up directory entries by their name as stored on disk, on any platform.
/// </summary>
/// <remarks>
/// Caches directory listings, so the owner must report every change it makes to a directory.
/// Treats names that differ only in upper/lower case or Unicode normalization form as aliases, even if the file system does not.
/// </remarks>
internal class DirectoryListingCache
{
    /// <summary>
    /// Maps directory paths to the names of their entries, grouped by <see cref="Fold"/>.
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, List<string>>> _listings = new(StringComparer.Ordinal);

    /// <summary>
    /// Looks up a directory entry without following links.
    /// </summary>
    /// <param name="fullPath">The fully qualified, normalized path of the entry.</param>
    /// <returns>
    /// The name as stored on disk and the attributes; <c>null</c> if the entry does not exist.
    /// The name differs from the one in <paramref name="fullPath"/> if it only exists under an alias. It is empty if the alias could not be identified.
    /// </returns>
    /// <exception cref="IOException">The lookup failed.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the parent directory was denied.</exception>
    public (string Name, FileAttributes Attributes)? TryGetDirectoryEntry(string fullPath)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(fullPath)) throw new ArgumentNullException(nameof(fullPath));
        #endregion

        string parent = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException($"'{fullPath}' has no parent directory.", nameof(fullPath));
        string name = Path.GetFileName(fullPath);

        if (TryGetCandidates(parent, name, refresh: false) is {} cached) return cached;

        // Not in the cached listing; might be a stale listing or a kind of aliasing that Fold() does not cover
        if (TryGetAttributes(fullPath) is not {} attributes) return null;
        return TryGetCandidates(parent, name, refresh: true) ?? (string.Empty, attributes);
    }

    private (string Name, FileAttributes Attributes)? TryGetCandidates(string parent, string name, bool refresh)
    {
        if (GetListing(parent, refresh) is not {} listing || !listing.TryGetValue(Fold(name), out var candidates)) return null;

        string storedName = candidates.Contains(name) ? name : candidates[0];
        if (TryGetAttributes(Path.Combine(parent, storedName)) is {} attributes) return (storedName, attributes);
        return null;
    }

    /// <summary>
    /// Gets the cached listing of a directory, listing it if necessary.
    /// </summary>
    /// <returns><c>null</c> if the directory does not exist.</returns>
    private Dictionary<string, List<string>>? GetListing(string directory, bool refresh)
    {
        if (!refresh && _listings.TryGetValue(directory, out var listing)) return listing;

        if (!Directory.Exists(directory))
        {
            _listings.Remove(directory);
            return null;
        }

        listing = new(StringComparer.Ordinal);
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            Add(listing, Path.GetFileName(entry));
        return _listings[directory] = listing;
    }

    /// <summary>
    /// Reports that an entry and possibly some of its parent directories, up to <paramref name="root"/>, have been created.
    /// </summary>
    public void Added(string fullPath, string root)
    {
        for (string current = fullPath; current.Length > root.Length && Path.GetDirectoryName(current) is {} parent; current = parent)
        {
            if (_listings.TryGetValue(parent, out var listing)) Add(listing, Path.GetFileName(current));
        }
    }

    /// <summary>
    /// Reports that an entry (and, if it was a directory, everything in it) has been removed.
    /// </summary>
    public void Removed(string fullPath)
    {
        if (Path.GetDirectoryName(fullPath) is {} parent && _listings.TryGetValue(parent, out var listing))
        {
            string name = Path.GetFileName(fullPath);
            string key = Fold(name);
            if (listing.TryGetValue(key, out var candidates) && candidates.Remove(name) && candidates.Count == 0)
                listing.Remove(key);
        }

        string prefix = fullPath + Path.DirectorySeparatorChar;
        foreach (string directory in _listings.Keys.Where(x => x == fullPath || x.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            _listings.Remove(directory);
    }

    private static void Add(Dictionary<string, List<string>> listing, string name)
    {
        string key = Fold(name);
        if (!listing.TryGetValue(key, out var candidates)) listing[key] = candidates = [];
        if (!candidates.Contains(name)) candidates.Add(name);
    }

    /// <summary>
    /// Maps all names that differ only in upper/lower case or Unicode normalization form to the same key.
    /// </summary>
    private static string Fold(string name)
    {
        try
        {
            name = name.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            // Invalid UTF-16 (e.g., unpaired surrogates) cannot be normalized
        }
        return name.ToUpperInvariant();
    }

    private static FileAttributes? TryGetAttributes(string fullPath)
    {
        try
        {
            return File.GetAttributes(fullPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }
}
