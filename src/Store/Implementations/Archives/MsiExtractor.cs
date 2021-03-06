// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Deployment.Compression.Cab;
using Microsoft.Deployment.WindowsInstaller;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a Windows Installer MSI package (with one or more embedded CAB archives).
    /// </summary>
    public class MsiExtractor : MicrosoftExtractor
    {
        #region Database
        private readonly Database _database;

        /// <summary>
        /// Prepares to extract a Windows Installer MSI package contained in a stream.
        /// </summary>
        /// <param name="archivePath">The path of the Windows Installer MSI package to be extracted.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The package is damaged.</exception>
        internal MsiExtractor(string archivePath, string targetPath)
            : base(targetPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(archivePath)) throw new ArgumentNullException(nameof(archivePath));
            #endregion

            try
            {
                _database = new(archivePath, DatabaseOpenMode.ReadOnly);
                ReadDirectories();
                ReadFiles();
                ReadCabinets();

                UnitsTotal = _files.Values.Sum(x => x.Size);
            }
            #region Error handling
            catch (InstallerException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        public override void Dispose()
        {
            try
            {
                _database.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
        #endregion

        #region Directories
        private record MsiDirectory(string Name, string ParentId)
        {
            public string? FullPath;
        }

        private readonly Dictionary<string, MsiDirectory> _directories = new();

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
        #endregion

        #region Files
        private record MsiFile(string Name, int Size, string DirectoryId)
        {
            public string? FullPath;
        }

        private readonly Dictionary<string, MsiFile> _files = new();

        private void ReadFiles()
        {
            using (var fileView = _database.OpenView("SELECT File, FileName, FileSize, Directory_ FROM File, Component WHERE Component_ = Component"))
            {
                fileView.Execute();
                foreach (var row in fileView)
                {
                    _files.Add(
                        row["File"].ToString(),
                        new MsiFile(
                            Name: row["FileName"].ToString().Split(':').Last().Split('|').Last(),
                            Size: (int)row["FileSize"],
                            DirectoryId: row["Directory_"].ToString()
                        ));
                }
            }

            foreach (var file in _files.Values)
                ResolveFile(file);
        }

        private void ResolveFile(MsiFile file) => file.FullPath = _directories[file.DirectoryId].FullPath + "/" + file.Name;
        #endregion

        #region Cabinets
        private List<string>? _cabinets;

        private void ReadCabinets()
        {
            using var mediaView = _database.OpenView("SELECT Cabinet FROM Media");
            mediaView.Execute();
            _cabinets = mediaView.Select(row => row["Cabinet"].ToString())
                                 .Where(name => name.StartsWith("#"))
                                 .Select(name => name[1..])
                                 .ToList();
        }
        #endregion

        /// <inheritdoc/>
        protected override void ExtractArchive()
        {
            try
            {
                foreach (string cabinet in _cabinets ?? throw new InvalidOperationException($"Must run {nameof(ReadCabinets)}() first."))
                {
                    using var streamsView = _database.OpenView("SELECT Data FROM _Streams WHERE Name = '{0}'", cabinet);
                    streamsView.Execute();
                    using var record = streamsView.Fetch();
                    if (record == null) throw new IOException(Resources.ArchiveInvalid + Environment.NewLine + $"Cabinet stream '{cabinet}' missing");

                    using var stream = record.GetStream("Data");
                    ExtractCab(stream);
                }
            }
            #region Error handling
            catch (InstallerException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            catch (CabException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        private void ExtractCab(Stream stream)
        {
            using var tempFile = new TemporaryFile("0install");
            // Extract embedded CAB from MSI
            using (var tempStream = File.Create(tempFile))
                stream.CopyToEx(tempStream);

            // Extract individual files from CAB
            using (CabStream = File.OpenRead(tempFile))
                CabEngine.Unpack(this, _ => true);
        }

        /// <inheritdoc/>
        protected override string? GetRelativePath(string entryName)
        {
            try
            {
                string? fullPath = _files[entryName].FullPath;
                return fullPath == null ? null : base.GetRelativePath(fullPath);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
    }
}
#endif
