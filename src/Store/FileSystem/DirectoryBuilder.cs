// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Streams;
using NanoByte.Common.Threading;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Builds a file system directory on-disk.
/// </summary>
public class DirectoryBuilder : MarshalNoTimeout, IBuilder
{
    /// <summary>
    /// The path to the directory to build the implementation in.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// A directory all hardlink targets must be a child of.
    /// Defaults to <see cref="Path"/>.
    /// </summary>
    public string AllowedHardlinkRoot { get; init; }

    private readonly IBuilder? _innerBuilder;

    /// <summary>
    /// Creates a new builder.
    /// </summary>
    /// <param name="path">The path to the directory to build the implementation in.</param>
    /// <param name="innerBuilder">An additional <see cref="IBuilder"/> to pass all calls on to as well. Usually <see cref="ManifestBuilder"/>.</param>
    public DirectoryBuilder(string path, IBuilder? innerBuilder = null)
    {
        AllowedHardlinkRoot = Path = path ?? throw new ArgumentNullException(nameof(path));
        _innerBuilder = innerBuilder;
    }

    /// <inheritdoc/>
    public void AddDirectory(string path)
    {
        Directory.CreateDirectory(GetFullPath(path));

        _innerBuilder?.AddDirectory(path);
    }

    /// <inheritdoc/>
    public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
    {
        string fullPath = GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);

        // Delete any preexisting file to reset permissions, etc.
        if (File.Exists(fullPath)) File.Delete(fullPath);

        using (var fileStream = FileUtils.Create(fullPath, Math.Max(0, stream.Length)))
        {
            if (_innerBuilder == null)
                stream.CopyToEx(fileStream);
            else
                _innerBuilder.AddFile(path, new ShadowingStream(stream, fileStream), modifiedTime, executable);
        }
        File.SetLastWriteTimeUtc(fullPath, modifiedTime);

        if (executable) ImplFileUtils.SetExecutable(fullPath);
    }

    /// <inheritdoc/>
    public void AddHardlink(string path, string target, bool executable = false)
    {
        string sourceAbsolute = GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sourceAbsolute)!);
        string targetAbsolute = GetFullPath(target, AllowedHardlinkRoot);

        FileUtils.CreateHardlink(sourceAbsolute, targetAbsolute);
        if (executable) ImplFileUtils.SetExecutable(targetAbsolute);

        _innerBuilder?.AddHardlink(path, target, executable);
    }

    /// <inheritdoc/>
    public void AddSymlink(string path, string target)
    {
        string sourceAbsolute = GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sourceAbsolute)!);

        // Delete any preexisting file to reset permissions, etc.
        if (File.Exists(sourceAbsolute)) File.Delete(sourceAbsolute);

        ImplFileUtils.CreateSymlink(sourceAbsolute, target);

        _innerBuilder?.AddSymlink(path, target);
    }

    /// <inheritdoc/>
    public void Rename(string path, string target)
    {
        string fullSourcePath = GetFullPath(path);
        string fullTargetPath = GetFullPath(target);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullTargetPath)!);

        if (File.Exists(fullSourcePath))
            File.Move(fullSourcePath, fullTargetPath);
        else if (Directory.Exists(fullSourcePath))
            Directory.Move(fullSourcePath, fullTargetPath);
        else throw new IOException(string.Format(Resources.FileOrDirNotFound, path));

        _innerBuilder?.Rename(path, target);
    }

    /// <inheritdoc/>
    public void Remove(string path)
    {
        string fullPath = GetFullPath(path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        else if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
        else throw new IOException(string.Format(Resources.FileOrDirNotFound, path));

        _innerBuilder?.Remove(path);
    }

    /// <inheritdoc/>
    public void MarkAsExecutable(string path)
    {
        ImplFileUtils.SetExecutable(GetFullPath(path));
        _innerBuilder?.MarkAsExecutable(path);
    }

    /// <inheritdoc/>
    public void TurnIntoSymlink(string path)
    {
        string fullPath = GetFullPath(path);

        if (!FileUtils.IsSymlink(fullPath))
            AddSymlink(path, File.ReadAllText(fullPath, Encoding.UTF8));
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
    /// <exception cref="IOException"><paramref name="relativePath"/> is invalid (e.g. is absolute, lies outside of <paramref name="allowedRoot"/>, contains invalid characters).</exception>
    private string GetFullPath(string relativePath, string allowedRoot)
    {
        if (Manifest.RejectPath(relativePath)) throw new IOException(string.Format(Resources.InvalidPath, relativePath));

        string fullPath;
        try
        {
            fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, relativePath));
        }
        #region Error handling
        catch (ArgumentException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(ex.Message, ex);
        }
        #endregion

        if (!fullPath.StartsWith(allowedRoot + System.IO.Path.DirectorySeparatorChar))
            throw new IOException(string.Format(Resources.InvalidPath, relativePath));

        return fullPath;
    }
}
