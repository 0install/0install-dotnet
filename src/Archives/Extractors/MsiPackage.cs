// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using Microsoft.Deployment.WindowsInstaller;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Provides access to files stored within a Windows Installer package (.msi).
/// </summary>
internal sealed class MsiPackage : IDisposable
{
    private readonly Database _database;

    /// <summary>
    /// Opens a Windows Installer database (.msi).
    /// </summary>
    /// <param name="path">The path of the .msi file.</param>
    public MsiPackage(string path)
    {
        _database = new(path, DatabaseOpenMode.ReadOnly);
        ReadDirectories();
        ReadFiles();
        ReadCabinets();
    }

    private readonly Dictionary<string, MsiDirectory> _directories = new();

    private record MsiDirectory(string Name, string ParentId)
    {
        public string? FullPath;
    }

    private void ReadDirectories()
    {
        using (var directoryView = _database.OpenView("SELECT Directory, DefaultDir, Directory_Parent FROM Directory"))
        {
            directoryView.Execute();
            foreach (var row in directoryView)
            {
                _directories.Add(
                    row["Directory"].ToString(),
                    new MsiDirectory(
                        Name: row["DefaultDir"].ToString().Split(':').Last().Split('|').Last(),
                        ParentId: row["Directory_Parent"].ToString()
                    ));
            }
        }

        foreach (var directory in _directories.Values)
            ResolveDirectory(directory);
    }

    private void ResolveDirectory(MsiDirectory directory)
    {
        if (directory.FullPath != null) return;

        if (string.IsNullOrEmpty(directory.ParentId))
        { // Root directory
            directory.FullPath = directory.Name;
        }
        else
        {
            var parent = _directories[directory.ParentId];
            if (parent == directory)
            { // Root directory
                directory.FullPath = directory.Name;
            }
            else
            { // Child directory
                ResolveDirectory(parent);
                directory.FullPath = (directory.Name == ".")
                    ? parent.FullPath
                    : parent.FullPath + "/" + directory.Name;
            }
        }
    }


    private readonly Dictionary<string, string> _files = new();

    private void ReadFiles()
    {
        using var fileView = _database.OpenView("SELECT File, FileName, FileSize, Directory_ FROM File, Component WHERE Component_ = Component");
        fileView.Execute();
        foreach (var row in fileView)
        {
            string? directory = _directories[row["Directory_"].ToString()].FullPath;
            string fileName = row["FileName"].ToString().Split(':').Last().Split('|').Last();
            _files.Add(row["File"].ToString(), directory + "/" + fileName);
        }
    }

    /// <summary>
    /// Map from CAB file names to on-disk file names.
    /// </summary>
    public IReadOnlyDictionary<string, string> Files => _files;

    private readonly List<string> _cabinets = new();

    private void ReadCabinets()
    {
        using var mediaView = _database.OpenView("SELECT Cabinet FROM Media");
        mediaView.Execute();
        _cabinets.Add(
            mediaView.Select(row => row["Cabinet"].ToString())
                     .Where(name => name.StartsWith("#"))
                     .Select(name => name[1..]));
    }

    /// <summary>
    /// Runs a <paramref name="callback"/> once for each embedded Cabinet (.cab file).
    /// </summary>
    public void ForEachCabinet([InstantHandle] Action<Stream> callback)
    {
        foreach (string cabinet in _cabinets)
        {
            using var streamsView = _database.OpenView("SELECT Data FROM _Streams WHERE Name = '{0}'", cabinet);
            streamsView.Execute();

            using var record = streamsView.Fetch();
            if (record == null) throw new IOException(Resources.ArchiveInvalid + Environment.NewLine + $"Cabinet stream '{cabinet}' missing");

            using var stream = record.GetStream("Data");
            callback(stream);
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _database.Dispose();
}
#endif
