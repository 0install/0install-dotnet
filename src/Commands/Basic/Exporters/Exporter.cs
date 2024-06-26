// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using ZeroInstall.Archives.Builders;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Icons;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Commands.Basic.Exporters;

/// <summary>
/// Exports feeds and implementations listed in a <see cref="Selections"/> document.
/// </summary>
public class Exporter
{
    private readonly Selections _selections;
    private readonly Architecture _architecture;
    private readonly string _destination, _contentDir;

    /// <summary>
    /// Creates a new exporter.
    /// </summary>
    /// <param name="selections">A list of <see cref="ImplementationSelection"/>s to check for referenced feeds.</param>
    /// <param name="architecture">The <see cref="Architecture"/> the <see cref="Selections"/> were generated for.</param>
    /// <param name="destination">The path of the directory to export to.</param>
    /// <exception cref="IOException">The directory <paramref name="destination"/> could not be created.</exception>
    /// <exception cref="UnauthorizedAccessException">Creating the directory <paramref name="destination"/> is not permitted.</exception>
    public Exporter(Selections selections,  Architecture architecture, string destination)
    {
        _selections = selections;
        _architecture = architecture;
        _destination = destination;
        _contentDir = Path.Combine(_destination, "content");
        Directory.CreateDirectory(_contentDir);
    }

    /// <summary>
    /// Exports all feeds listed in a <see cref="Selections"/> document along with any OpenPGP public key files required for validation.
    /// </summary>
    /// <param name="feedCache">Used to get local feed files.</param>
    /// <param name="openPgp">Used to get export keys feeds were signed with.</param>
    /// <exception cref="IOException">A feed or GnuPG could not be read from the cache.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or access to a file is not permitted.</exception>
    public void ExportFeeds(IFeedCache feedCache, IOpenPgp openPgp)
    {
        #region Sanity checks
        if (feedCache == null) throw new ArgumentNullException(nameof(feedCache));
        if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
        #endregion

        var feedUris = _selections.Implementations
                                  .SelectMany(x => new [] {x.InterfaceUri, x.FromFeed})
                                  .WhereNotNull().Distinct().ToList();

        foreach (var feedUri in feedUris)
        {
            string filePath = Path.Combine(_contentDir, feedUri.Escape());
            if (!filePath.EndsWith(".xml")) filePath += ".xml";

            if (feedCache.GetPath(feedUri) is {} path)
            {
                Log.Info($"Exporting feed {feedUri.ToStringRfc()}");
                File.Copy(path, filePath, overwrite: true);
            }
        }

        foreach (var signature in feedUris.SelectMany(feedCache.GetSignatures).OfType<ValidSignature>().Distinct())
        {
            Log.Info($"Exporting GPG key {signature.FormatKeyID()}");
            openPgp.DeployPublicKey(signature, _contentDir);
        }
    }

    /// <summary>
    /// Exports all implementations listed in a <see cref="Selections"/> document as archives.
    /// </summary>
    /// <param name="implementationStore">Used to get cached implementations.</param>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">An implementation archive could not be created.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or access to a file is not permitted.</exception>
    public void ExportImplementations(IImplementationStore implementationStore, ITaskHandler handler)
    {
        #region Sanity checks
        if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        foreach (var digest in _selections.Implementations.Select(x => x.ManifestDigest).Where(x => x.Best != null).Distinct())
        {
            if (implementationStore.GetPath(digest) is {} sourcePath)
                ArchiveBuilder.RunForDirectory(sourcePath, Path.Combine(_contentDir, $"{digest.Best}.tgz"), Archive.MimeTypeTarGzip, handler);
            else
                Log.Warn($"Implementation {digest} missing from cache");
        }
    }

    /// <summary>
    /// Exports all specified icons.
    /// </summary>
    /// <param name="icons">The icons to export.</param>
    /// <param name="iconStore">The icon store to export the icons from.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while reading or writing a file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or access to a file is not permitted.</exception>
    /// <exception cref="WebException">A problem occurred while downloading icons.</exception>
    public void ExportIcons(IEnumerable<Icon> icons, IIconStore iconStore)
    {
        #region Sanity checks
        if (icons == null) throw new ArgumentNullException(nameof(icons));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        foreach (var icon in icons)
        {
            File.Copy(
                iconStore.GetFresh(icon),
                Path.Combine(_contentDir, IconStore.GetFileName(icon)),
                overwrite: true);
        }
    }

    /// <summary>
    /// Deploys a script for importing exported feeds and implementations.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while writing the script.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the script is not permitted.</exception>
    public void DeployImportScript()
    {
        string fileName = (_architecture.OS == OS.Windows) ? "import.cmd" : "import.sh";
        string target = Path.Combine(_destination, fileName);

        typeof(Exporter).CopyEmbeddedToFile(fileName, target);
        if (UnixUtils.IsUnix)
            UnixUtils.SetExecutable(target, executable: true);
    }
}
