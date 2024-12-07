// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using NanoByte.Common.Threading;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Store.Manifests;

/// <summary>
/// Builds a <see cref="Manifest"/> for a file system directory.
/// </summary>
/// <param name="format">The manifest format.</param>
public class ManifestBuilder(ManifestFormat format) : MarshalNoTimeout, IBuilder
{
    /// <summary>
    /// The manifest.
    /// </summary>
    public Manifest Manifest { get; } = new(format);

    /// <inheritdoc/>
    public void AddDirectory(string path)
        => Manifest.Add(path.ToUnixPath());

    /// <inheritdoc/>
    public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
    {
        (string dir, string file) = Split(path);

        if (stream.Length < 0)
        {
            // Determine length of stream by counting how many bytes were read
            stream = new ProgressStream(stream, new SynchronousProgress<long>());
        }

        string digest = Manifest.Format.DigestContent(stream);

        var manifestDir = Manifest[dir];

        const string appleDoublePrefix = "._";
        if (file.StartsWith(appleDoublePrefix, out var rest) && manifestDir.ContainsKey(rest))
        {
            Log.Debug($"Ignoring AppleDouble file '{path}' in manifest");
            return;
        }
        if (manifestDir.Remove(appleDoublePrefix + file))
            Log.Debug($"Ignoring AppleDouble file '{Path.Combine(dir, appleDoublePrefix + "path")}' in manifest");

        manifestDir[file] = executable
            ? new ManifestExecutableFile(digest, modifiedTime, stream.Length)
            : new ManifestNormalFile(digest, modifiedTime, stream.Length);
    }

    /// <inheritdoc/>
    public void AddHardlink(string path, string target, bool executable = false)
    {
        (string dir, string file) = Split(path);
        (string existingDir, string existingFile) = Split(target);

        if (Manifest[existingDir].TryGetValue(existingFile, out var result))
            Manifest[dir][file] = result;
        else throw new IOException(string.Format(Resources.FileOrDirNotFound, path));
    }

    /// <inheritdoc/>
    public void AddSymlink(string path, string target)
    {
        (string dir, string file) = Split(path);
        using var stream = target.ToStream();
        Manifest[dir][file] = new ManifestSymlink(Manifest.Format.DigestContent(stream), stream.Length);
    }

    /// <inheritdoc/>
    public void Rename(string path, string target)
    {
        (string sourceDir, string sourceFile) = Split(path);
        if (Manifest.TryGetValue(sourceDir, out var directory))
        {
            if (directory.TryGetValue(sourceFile, out var file))
            {
                (string targetDir, string targetFile) = Split(target);
                Manifest[targetDir][targetFile] = file;
                directory.Remove(sourceFile);
                return;
            }
        }

        if (!Manifest.Rename(path.ToUnixPath(), target.ToUnixPath()))
            throw new IOException(string.Format(Resources.FileOrDirNotFound, path));
    }

    /// <inheritdoc/>
    public void Remove(string path)
    {
        (string dir, string file) = Split(path);
        if (Manifest.TryGetValue(dir, out var directory))
        {
            if (directory.Remove(file))
                return;
        }

        if (!Manifest.Remove(path.ToUnixPath()))
            throw new IOException(string.Format(Resources.FileOrDirNotFound, path));
    }

    /// <inheritdoc/>
    public void MarkAsExecutable(string path)
    {
        (string dir, string file) = Split(path);
        (string digest, UnixTime modifiedTime, long size) = (ManifestFile)Manifest[dir][file];
        Manifest[dir][file] = new ManifestExecutableFile(digest, modifiedTime, size);
    }

    /// <inheritdoc/>
    public void TurnIntoSymlink(string path)
    {
        (string dir, string file) = Split(path);
        (string digest, long size) = Manifest[dir][file];
        Manifest[dir][file] = new ManifestSymlink(digest, size);
    }

    private static (string dir, string file) Split(string path)
    {
        if (Manifest.RejectPath(path)) throw new IOException(string.Format(Resources.InvalidPath, path));

        int lastSeparator = path.LastIndexOf(Path.DirectorySeparatorChar);
        return (
            path[..Math.Max(0, lastSeparator)].ToUnixPath(),
            path[(lastSeparator + 1)..]
        );
    }
}
