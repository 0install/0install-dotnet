// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using NanoByte.Common.Threading;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Builds a file system directory on-disk.
/// </summary>
/// <param name="path">The path to the directory to build the implementation in.</param>
/// <param name="innerBuilder">An additional <see cref="IBuilder"/> to pass all calls on to as well. Usually <see cref="ManifestBuilder"/>.</param>
public class DirectoryBuilder(string path, IBuilder? innerBuilder = null) : MarshalNoTimeout, IBuilder
{
    /// <summary>
    /// The path to the directory to build the implementation in.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// A directory all hardlink targets must be a child of.
    /// Defaults to <see cref="Path"/>.
    /// </summary>
    public string AllowedHardlinkRoot { get; init; } = path;

    /// <summary>
    /// The maximum size of a file that can be turned into a symlink with <see cref="TurnIntoSymlink"/>.
    /// </summary>
    public const long MaxSymlinkTargetSize = 64 * 1024;

    /// <inheritdoc/>
    public void AddDirectory(string path)
    {
        string fullPath = GetFullPath(path);
        if (IsLink(fullPath)) throw new IOException(string.Format(Resources.InvalidPath, path));
        Directory.CreateDirectory(fullPath);

        innerBuilder?.AddDirectory(path);
    }

    /// <inheritdoc/>
    public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
    {
        string fullPath = GetFullPath(path);
        Directory.CreateDirectory(Paths.Parent(fullPath));

        // Delete any preexisting file to reset permissions, etc.
        DeleteFileOrLink(fullPath);

        using (var fileStream = FileUtils.Create(fullPath, Math.Max(0, stream.Length)))
        {
            if (innerBuilder == null)
                stream.CopyToEx(fileStream);
            else
                innerBuilder.AddFile(path, new ShadowingStream(stream, fileStream), modifiedTime, executable);
        }
        File.SetLastWriteTimeUtc(fullPath, modifiedTime);

        if (executable) ImplFileUtils.SetExecutable(fullPath);
    }

    /// <inheritdoc/>
    public void AddHardlink(string path, string target, bool executable = false)
    {
        string sourceAbsolute = GetFullPath(path);
        Directory.CreateDirectory(Paths.Parent(sourceAbsolute));
        string targetAbsolute = GetFullPath(target, AllowedHardlinkRoot);
        EnsureRegularFile(targetAbsolute, target);

        // Do not trust the caller to provide the correct value for 'executable'
        executable = ImplFileUtils.IsExecutable(targetAbsolute);

        FileUtils.CreateHardlink(sourceAbsolute, targetAbsolute);
        if (executable) ImplFileUtils.SetExecutable(targetAbsolute);

        innerBuilder?.AddHardlink(path, target, executable);
    }

    /// <inheritdoc/>
    public bool TryAddExternalHardlink(string path, FileInfo target, bool executable = false)
    {
        if (!target.FullName.StartsWith(AllowedHardlinkRoot + System.IO.Path.DirectorySeparatorChar))
            return false;
        EnsureNoLinksInParents(target.FullName, AllowedHardlinkRoot);
        EnsureRegularFile(target.FullName, target.FullName);

        string sourceAbsolute = GetFullPath(path);
        Directory.CreateDirectory(Paths.Parent(sourceAbsolute));

        try
        {
            FileUtils.CreateHardlink(sourceAbsolute, target.FullName);
        }
        catch (NotSupportedException)
        {
            return false;
        }

        if (executable) ImplFileUtils.SetExecutable(target.FullName);

        if (innerBuilder != null)
        {
            using var stream = target.OpenRead();
            innerBuilder.AddFile(path, stream, target.LastWriteTimeUtc, executable);
        }

        return true;
    }

    /// <inheritdoc/>
    public void AddSymlink(string path, string target)
    {
        string sourceAbsolute = GetFullPath(path);
        Directory.CreateDirectory(Paths.Parent(sourceAbsolute));

        // Delete any preexisting file to reset permissions, etc.
        DeleteFileOrLink(sourceAbsolute);

        ImplFileUtils.CreateSymlink(sourceAbsolute, target);

        innerBuilder?.AddSymlink(path, target);
    }

    /// <inheritdoc/>
    public void Rename(string path, string target)
    {
        string fullSourcePath = GetFullPath(path);
        string fullTargetPath = GetFullPath(target);
        Directory.CreateDirectory(Paths.Parent(fullTargetPath));

        if (File.Exists(fullSourcePath))
            File.Move(fullSourcePath, fullTargetPath);
        else if (Directory.Exists(fullSourcePath))
            Directory.Move(fullSourcePath, fullTargetPath);
        else throw new IOException(string.Format(Resources.FileOrDirNotFound, path));

        innerBuilder?.Rename(path, target);
    }

    /// <inheritdoc/>
    public void Remove(string path)
    {
        string fullPath = GetFullPath(path);
        if (IsLink(fullPath))
            DeleteFileOrLink(fullPath);
        else if (File.Exists(fullPath))
            File.Delete(fullPath);
        else if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
        else throw new IOException(string.Format(Resources.FileOrDirNotFound, path));

        innerBuilder?.Remove(path);
    }

    /// <inheritdoc/>
    public void MarkAsExecutable(string path)
    {
        string fullPath = GetFullPath(path);
        EnsureRegularFile(fullPath, path);

        ImplFileUtils.SetExecutable(fullPath);
        innerBuilder?.MarkAsExecutable(path);
    }

    /// <inheritdoc/>
    public void TurnIntoSymlink(string path)
    {
        string fullPath = GetFullPath(path);
        if (FileUtils.IsSymlink(fullPath)) return;

        EnsureRegularFile(fullPath, path);
        if (new FileInfo(fullPath).Length > MaxSymlinkTargetSize) throw new IOException(string.Format(Resources.InvalidPath, path));

        string target = File.ReadAllText(fullPath, Encoding.UTF8);
        if (!ImplFileUtils.IsValidSymlinkTarget(target)) throw new IOException(string.Format(Resources.InvalidSymlinkTarget, path));

        AddSymlink(path, target);
    }

    /// <summary>
    /// Resolves a path relative to <see cref="Path"/> to a full path.
    /// </summary>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <exception cref="IOException"><paramref name="relativePath"/> is invalid (e.g. is absolute, lies outside of <see cref="Path"/>, contains invalid characters).</exception>
    private string GetFullPath(string relativePath)
        => GetFullPath(relativePath, allowedRoot: Path);

    /// <summary>
    /// Resolves a path relative to <see cref="Path"/> to a full path.
    /// </summary>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <param name="allowedRoot">A directory the resulting path must be a child of.</param>
    /// <exception cref="IOException"><paramref name="relativePath"/> is invalid (e.g. is absolute, lies outside of <paramref name="allowedRoot"/>, contains invalid characters, passes through a link).</exception>
    private string GetFullPath(string relativePath, string allowedRoot)
    {
        if (Manifest.RejectPath(relativePath)) throw new IOException(string.Format(Resources.InvalidPath, relativePath));

        // Colons would address NTFS alternate data streams (possibly of a link's target)
        if (WindowsUtils.IsWindows && relativePath.Contains(":")) throw new IOException(string.Format(Resources.InvalidPath, relativePath));

        string fullPath = Paths.Absolute(Paths.Combine(Path, relativePath));

        if (!fullPath.StartsWith(allowedRoot + System.IO.Path.DirectorySeparatorChar))
            throw new IOException(string.Format(Resources.InvalidPath, relativePath));

        EnsureNoLinksInParents(fullPath, allowedRoot);

        return fullPath;
    }

    /// <summary>
    /// Ensures that no existing directory between <paramref name="root"/> and <paramref name="fullPath"/> is a link (symlink, junction, etc.) or a file.
    /// Prevents writing, reading or deleting outside of <paramref name="root"/> via links created by previous build steps.
    /// </summary>
    /// <exception cref="IOException">A parent directory of <paramref name="fullPath"/> is a link or a file.</exception>
    private static void EnsureNoLinksInParents(string fullPath, string root)
    {
        string[] parts = fullPath[(root.Length + 1)..].Split(System.IO.Path.DirectorySeparatorChar);
        string current = root;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            current = System.IO.Path.Combine(current, parts[i]);
            if (TryGetAttributes(current) is not {} attributes) return; // Remaining directories do not exist yet and will be created as needed

            if (attributes.HasFlag(FileAttributes.ReparsePoint) || !attributes.HasFlag(FileAttributes.Directory))
                throw new IOException(string.Format(Resources.InvalidPath, fullPath));
        }
    }

    /// <summary>
    /// Ensures that <paramref name="fullPath"/> is an existing regular file and not a link or directory.
    /// </summary>
    /// <exception cref="IOException"><paramref name="fullPath"/> does not exist or is not a regular file.</exception>
    private static void EnsureRegularFile(string fullPath, string displayPath)
    {
        var attributes = TryGetAttributes(fullPath) ?? throw new IOException(string.Format(Resources.FileOrDirNotFound, displayPath));
        if (attributes.HasFlag(FileAttributes.ReparsePoint) || attributes.HasFlag(FileAttributes.Directory))
            throw new IOException(string.Format(Resources.InvalidPath, displayPath));
    }

    /// <summary>
    /// Determines whether <paramref name="fullPath"/> is a link (symlink, junction, etc.) without following it.
    /// </summary>
    private static bool IsLink(string fullPath)
        => TryGetAttributes(fullPath) is {} attributes && attributes.HasFlag(FileAttributes.ReparsePoint);

    /// <summary>
    /// Deletes a file or a link (without following it) if it exists. Does not delete regular directories.
    /// </summary>
    private static void DeleteFileOrLink(string fullPath)
    {
        if (TryGetAttributes(fullPath) is not {} attributes) return;

        if (!attributes.HasFlag(FileAttributes.Directory)) File.Delete(fullPath);
        else if (attributes.HasFlag(FileAttributes.ReparsePoint)) Directory.Delete(fullPath, recursive: false);
    }

    /// <summary>
    /// Gets the attributes of a file system entry without following links.
    /// </summary>
    /// <returns>The attributes; <c>null</c> if the entry does not exist.</returns>
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
