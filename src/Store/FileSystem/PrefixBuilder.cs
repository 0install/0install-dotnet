// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Threading;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Wraps an <see cref="IBuilder"/> and prepends a directory prefix to paths.
/// </summary>
/// <param name="underlyingBuilder">The underlying <see cref="IBuilder"/> to wrap.</param>
/// <param name="prefix">The directory prefix to prepend to paths.</param>
public class PrefixBuilder(IBuilder underlyingBuilder, string prefix) : MarshalNoTimeout, IBuilder
{
    /// <inheritdoc/>
    public void AddDirectory(string path)
        => underlyingBuilder.AddDirectory(GetPath(path));

    /// <inheritdoc/>
    public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
        => underlyingBuilder.AddFile(GetPath(path), stream, modifiedTime, executable);

    /// <inheritdoc/>
    public void AddHardlink(string path, string target, bool executable = false)
        => underlyingBuilder.AddHardlink(GetPath(path), Path.Combine(prefix, target), executable);

    /// <inheritdoc/>
    public void AddSymlink(string path, string target)
        => underlyingBuilder.AddSymlink(GetPath(path), target);

    /// <inheritdoc/>
    public void Rename(string path, string target)
        => underlyingBuilder.Rename(GetPath(path), Path.Combine(prefix, target));

    /// <inheritdoc/>
    public void Remove(string path)
        => underlyingBuilder.Remove(GetPath(path));

    /// <inheritdoc />
    public void MarkAsExecutable(string path)
        => underlyingBuilder.MarkAsExecutable(GetPath(path));

    /// <inheritdoc />
    public void TurnIntoSymlink(string path)
        => underlyingBuilder.TurnIntoSymlink(GetPath(path));

    /// <summary>
    /// Prepends the prefix to a <paramref name="path"/>.
    /// </summary>
    /// <exception cref="IOException">The prefix or the <paramref name="path"/> contain invalid characters.</exception>
    private string GetPath(string path)
    {
        try
        {
            return Path.Combine(prefix, path);
        }
        #region Error handling
        catch (ArgumentException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(ex.Message, ex);
        }
        #endregion
    }
}
