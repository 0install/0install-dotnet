// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Reads the content of a directory to an <see cref="IBuilder"/> using hardlinks instead of copying file contents.
/// </summary>
public class ReadDirectoryAsHardlinks : ReadDirectoryBase
{
    private readonly IForwardOnlyBuilder _builder;
    private readonly Manifest? _manifest;
    private readonly string _hardlinkRoot;

    /// <summary>
    /// Creates a new directory read task that creates hardlinks.
    /// </summary>
    /// <param name="path">The path of the directory to read.</param>
    /// <param name="builder">The builder to read to.</param>
    /// <param name="hardlinkRoot">The root directory that hardlink targets will be relative to.</param>
    /// <param name="name">A name describing the task in human-readable form.</param>
    public ReadDirectoryAsHardlinks(string path, IForwardOnlyBuilder builder, string hardlinkRoot, [Localizable(true)] string? name = null)
        : base(path)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _hardlinkRoot = (hardlinkRoot ?? throw new ArgumentNullException(nameof(hardlinkRoot))).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
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
        
        // Create hardlink to the source file
        string relativePath = file.RelativeTo(Source);
        string targetPath = GetRelativePathFromRoot(file.FullName);
        
        try
        {
            _builder.AddHardlink(relativePath, targetPath, executable);
        }
        catch (NotSupportedException)
        {
            // If hardlinks are not supported, fall back to copying the file
            using var stream = file.OpenRead();
            _builder.AddFile(relativePath, stream, file.LastWriteTimeUtc, executable);
        }
    }

    /// <summary>
    /// Gets the path of a file relative to the hardlink root.
    /// </summary>
    private string GetRelativePathFromRoot(string fullPath)
    {
        // Normalize both paths to prevent path traversal attacks
        string normalizedFullPath = System.IO.Path.GetFullPath(fullPath);
        string normalizedRoot = System.IO.Path.GetFullPath(_hardlinkRoot.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        
        // Ensure the file is actually under the hardlink root
        if (!normalizedFullPath.StartsWith(normalizedRoot + System.IO.Path.DirectorySeparatorChar))
            throw new IOException($"File {fullPath} is not under hardlink root {_hardlinkRoot}");

        // Remove the hardlink root prefix (including the trailing separator)
        return normalizedFullPath.Substring(normalizedRoot.Length + 1);
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
