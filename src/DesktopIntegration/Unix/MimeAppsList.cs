// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Edits a <c>mimeapps.list</c> file, which stores the default applications for MIME types. Preserves unrelated content.
/// </summary>
/// <seealso href="https://specifications.freedesktop.org/mime-apps-spec/latest/"/>
internal sealed class MimeAppsList(IEnumerable<string> lines)
{
    private const string DefaultsGroup = "[Default Applications]";

    private readonly List<string> _lines = lines.ToList();
    private bool _changed;

    /// <summary>
    /// Loads the file for the current user or the entire machine, edits it and saves it if there were any changes.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while accessing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the file is not permitted.</exception>
    public static void Edit(bool machineWide, Action<MimeAppsList> action)
    {
        string path = Paths.Combine(FreeDesktop.GetConfigDir(machineWide), "mimeapps.list");

        // Write to final symlink target (e.g., file managed by dotfiles repository) instead of replacing the symlink
        for (int i = 0; i < 40 && File.Exists(path) && FileUtils.IsSymlink(path, out string? target); i++) // Limit like Linux to handle loops
            path = Path.Combine(Paths.Parent(path), target);

        var list = new MimeAppsList(File.Exists(path) ? File.ReadAllLines(path) : []);
        action(list);
        if (!list._changed) return;

        using var atomic = new AtomicWrite(path);
        File.WriteAllText(atomic.WritePath, list.ToString());
        atomic.Commit();
    }

    /// <summary>
    /// Makes an application the default for a MIME type. Keeps existing defaults as fallbacks.
    /// </summary>
    /// <param name="mimeType">The MIME type.</param>
    /// <param name="desktopID">The file name of the application's Desktop Entry.</param>
    public void AddDefault(string mimeType, string desktopID)
    {
        int group = _lines.FindIndex(x => x.Trim() == DefaultsGroup);
        if (group == -1)
        {
            if (_lines.Count != 0 && _lines[^1].Trim().Length != 0) _lines.Add("");
            _lines.Add(DefaultsGroup);
            group = _lines.Count - 1;
        }

        int end = group + 1, lastEntry = group;
        for (; end < _lines.Count && !IsGroup(_lines[end]); end++)
        {
            if (_lines[end].Trim().Length != 0) lastEntry = end;
            if (TryParse(_lines[end]) is not var (key, ids) || key != mimeType) continue;

            if (ids.FirstOrDefault() != desktopID)
                SetLine(end, key, ids.Where(x => x != desktopID).Prepend(desktopID));
            return;
        }

        _lines.Insert(lastEntry + 1, Format(mimeType, [desktopID]));
        _changed = true;
    }

    /// <summary>
    /// Stops an application from being the default for a MIME type.
    /// </summary>
    /// <param name="mimeType">The MIME type.</param>
    /// <param name="desktopID">The file name of the application's Desktop Entry.</param>
    public void RemoveDefault(string mimeType, string desktopID)
        => Remove(desktopID, (group, key) => group == DefaultsGroup && key == mimeType);

    /// <summary>
    /// Removes all references to an application.
    /// </summary>
    /// <param name="desktopID">The file name of the application's Desktop Entry.</param>
    public void RemoveAll(string desktopID)
        => Remove(desktopID, (_, _) => true);

    private void Remove(string desktopID, Func<string?, string, bool> predicate)
    {
        string? group = null;
        for (int i = 0; i < _lines.Count; i++)
        {
            if (IsGroup(_lines[i])) group = _lines[i].Trim();
            else if (TryParse(_lines[i]) is var (key, ids) && ids.Contains(desktopID) && predicate(group, key))
            {
                if (!SetLine(i, key, ids.Where(x => x != desktopID))) i--;
            }
        }
    }

    private bool SetLine(int index, string key, IEnumerable<string> ids)
    {
        _changed = true;

        var list = ids.ToList();
        if (list.Count == 0)
        {
            _lines.RemoveAt(index);
            return false;
        }
        else
        {
            _lines[index] = Format(key, list);
            return true;
        }
    }

    private static bool IsGroup(string line) => line.TrimStart().StartsWithOrdinal("[");

    private static (string key, IReadOnlyList<string> ids)? TryParse(string line)
    {
        int separator = line.IndexOf('=');
        if (separator == -1 || line.TrimStart().StartsWithOrdinal("#")) return null;

        return (
            line[..separator].Trim(),
            line[(separator + 1)..].Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length != 0).ToList());
    }

    private static string Format(string key, IEnumerable<string> ids)
        => $"{key}={string.Join(";", ids)};";

    /// <summary>
    /// Returns the contents of the file.
    /// </summary>
    public override string ToString() => string.Concat(_lines.Select(x => x + "\n"));
}
