// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Reads the content of a directory to an <see cref="IBuilder"/>.
/// </summary>
public class ReadDirectory : ReadDirectoryBase
{
    private readonly IForwardOnlyBuilder _builder;
    private readonly Manifest? _manifest;

    /// <summary>
    /// Creates a new directory read task.
    /// </summary>
    /// <param name="path">The path of the directory to read.</param>
    /// <param name="builder">The builder to read to.</param>
    /// <param name="name">A name describing the task in human-readable form.</param>
    public ReadDirectory(string path, IForwardOnlyBuilder builder, [Localizable(true)] string? name = null)
        : base(path)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Name = name ?? string.Format(Resources.ReadDirectory, path);

        if (ShouldReadManifest(path))
        {
            _manifest = Manifest.TryLoad(
                Path.Combine(path, Manifest.ManifestFile),
                ManifestFormat.Sha1New); // Actual digest format does not matter because we are only interested in symlinks and executable bits
        }
    }

    private static bool ShouldReadManifest(string path)
    {
        try
        {
            // Symlinks and executable bits may be lost on non-Unix filesystems and can be reconstructed from a manifest file
            return !FileUtils.IsUnixFS(Path.Combine(path, ".."));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    protected override void HandleDirectory(DirectoryInfo entry)
    {
        if (entry.IsSymlink(out string? target))
            _builder.AddSymlink(entry.RelativeTo(Source), target);
        else
            _builder.AddDirectory(entry.RelativeTo(Source));
    }

    /// <inheritdoc/>
    protected override void HandleFile(FileInfo file, FileInfo? hardlinkTarget = null)
    {
        string path = file.RelativeTo(Source);
        if (!Manifest.RejectPath(path))
            _builder.AddFile(path, file, GetManifestElement(file), hardlinkTarget?.RelativeTo(Source));
    }

    /// <summary>
    /// Tries to get a <paramref name="file"/>'s equivalent entry in the <see cref="_manifest"/>.
    /// </summary>
    private ManifestElement? GetManifestElement(FileInfo file)
        => _manifest != null
        && _manifest.TryGetValue(file.Directory!.RelativeTo(Source).ToUnixPath(), out var dir)
        && dir.TryGetValue(file.Name, out var element)
            ? element
            : null;
}
