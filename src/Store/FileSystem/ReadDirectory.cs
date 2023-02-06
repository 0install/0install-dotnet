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

        bool shouldReadManifest;
        try
        {
            // Symlinks and executable bits may be lost on non-Unix filesystems and can be reconstructed from a manifest file
            shouldReadManifest = !FileUtils.IsUnixFS(Path.Combine(path, ".."));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            shouldReadManifest = true;
        }

        if (shouldReadManifest)
        {
            _manifest = Manifest.TryLoad(
                Path.Combine(path, Manifest.ManifestFile),
                ManifestFormat.Sha1New); // Actual digest format does not matter because we are only interested in symlinks and executable bits
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
        if (Manifest.RejectPath(file.RelativeTo(Source))) return;

        var element = GetManifestElement(file);

        if (ImplFileUtils.IsSymlink(file.FullName, out string? symlinkTarget, element))
        {
            _builder.AddSymlink(file.RelativeTo(Source), symlinkTarget);
            return;
        }

        if (!FileUtils.IsRegularFile(file.FullName))
            throw new NotSupportedException(string.Format(Resources.IllegalFileType, file.FullName));

        bool executable = ImplFileUtils.IsExecutable(file.FullName, element);
        try
        {
            if (hardlinkTarget != null)
            {
                _builder.AddHardlink(file.RelativeTo(Source), hardlinkTarget.RelativeTo(Source), executable);
                return;
            }
        }
        catch (NotSupportedException)
        {}

        using var stream = file.OpenRead();
        _builder.AddFile(file.RelativeTo(Source), stream, file.LastWriteTimeUtc, executable);
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
