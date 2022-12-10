// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common.Native;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Publish.Capture;

/// <summary>
/// Represents the differences between two <see cref="Snapshot"/>s. Extracts information about applications installed.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class SnapshotDiff : Snapshot
{
    /// <summary>
    /// Determines which elements have been added to the system between two snapshots.
    /// </summary>
    /// <param name="before">The first snapshot taken.</param>
    /// <param name="after">The second snapshot taken.</param>
    /// <remarks>Assumes that all internal arrays are sorted alphabetically.</remarks>
    public SnapshotDiff(Snapshot before, Snapshot after)
    {
        #region Sanity checks
        if (before == null) throw new ArgumentNullException(nameof(before));
        if (after == null) throw new ArgumentNullException(nameof(after));
        #endregion

        void FindDiff<T>(Func<Snapshot, List<T>> get, IComparer<T>? comparer = null)
            where T : IComparable<T>
            => get(this).Add(get(after).GetAddedElements(get(before), comparer ?? DefaultComparer<T>.Instance));

        FindDiff(x => x.ServiceAssocs);
        FindDiff(x => x.AutoPlayHandlersUser);
        FindDiff(x => x.AutoPlayHandlersMachine);
        FindDiff(x => x.AutoPlayAssocsUser);
        FindDiff(x => x.AutoPlayAssocsMachine);
        FindDiff(x => x.FileAssocs);
        FindDiff(x => x.ProtocolAssocs);
        FindDiff(x => x.ProgIDs, StringComparer.OrdinalIgnoreCase);
        FindDiff(x => x.ClassIDs, StringComparer.OrdinalIgnoreCase);
        FindDiff(x => x.RegisteredApplications);
        FindDiff(x => x.ContextMenuFiles);
        FindDiff(x => x.ContextMenuExecutableFiles);
        FindDiff(x => x.ContextMenuDirectories);
        FindDiff(x => x.ContextMenuAll);
        FindDiff(x => x.ProgramsDirs, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Locates the directory into which the new application was installed.
    /// </summary>
    /// <exception cref="InvalidOperationException">No installation directory was detected.</exception>
    public string GetInstallationDir()
    {
        string installationDir;
        if (ProgramsDirs.Count == 0)
            throw new InvalidOperationException(Resources.NoInstallationDirDetected);
        else
        {
            if (ProgramsDirs.Count > 1)
                Log.Warn(Resources.MultipleInstallationDirsDetected);

            installationDir = new DirectoryInfo(ProgramsDirs[0]).WalkThroughPrefix().FullName;
            Log.Info(string.Format(Resources.InstallationDirDetected, installationDir));
        }

        return installationDir;
    }

    /// <summary>
    /// Retrieves data about multiple verbs (executable commands) from the registry.
    /// </summary>
    /// <param name="typeKey">The registry key containing information about the file type / protocol the verbs belong to.</param>
    /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
    /// <returns>A list of detected <see cref="Verb"/>.</returns>
    private static IEnumerable<Verb> GetVerbs(RegistryKey typeKey, CommandMapper commandMapper)
    {
        #region Sanity checks
        if (typeKey == null) throw new ArgumentNullException(nameof(typeKey));
        if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
        #endregion

        return RegUtils.GetSubKeyNames(typeKey, "shell").Select(verbName => GetVerb(typeKey, commandMapper, verbName)).WhereNotNull();
    }

    /// <summary>
    /// Retrieves data about a verb (an executable command) from the registry.
    /// </summary>
    /// <param name="typeKey">The registry key containing information about the file type / protocol the verb belongs to.</param>
    /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
    /// <param name="verbName">The internal name of the verb.</param>
    /// <returns>The detected <see cref="Verb"/> or an empty <see cref="Verb"/> if no match was found.</returns>
    private static Verb? GetVerb(RegistryKey typeKey, CommandMapper commandMapper, string verbName)
    {
        #region Sanity checks
        if (typeKey == null) throw new ArgumentNullException(nameof(typeKey));
        if (string.IsNullOrEmpty(verbName)) throw new ArgumentNullException(nameof(verbName));
        if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
        #endregion

        using var verbKey = typeKey.TryOpenSubKey($@"shell\{verbName}");
        if (verbKey == null) return null;

        string? description = verbKey.GetValue("")?.ToString();
        string? commandLine;
        using (var commandKey = verbKey.TryOpenSubKey("command"))
        {
            if (commandKey == null) return null;
            commandLine = commandKey.GetValue("")?.ToString();
        }

        if (string.IsNullOrEmpty(commandLine)) return null;
        var command = commandMapper.GetCommand(commandLine, out string? additionalArgs);
        if (command == null) return null;

        var verb = new Verb
        {
            Name = verbName,
            Command = (command.Name == Command.NameRun) ? null : command.Name,
            ArgumentsLiteral = additionalArgs
        };
        if (!string.IsNullOrEmpty(description)) verb.Descriptions.Add(description);
        return verb;
    }
}
