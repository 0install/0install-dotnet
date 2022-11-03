// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Threading;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Wraps an <see cref="IBuilder"/> and prepends a directory prefix to paths.
/// </summary>
public class PrefixBuilder : MarshalNoTimeout, IBuilder
{
    private readonly IBuilder _underlyingBuilder;
    private readonly string _prefix;

    /// <summary>
    /// Creates a new prefix builder.
    /// </summary>
    /// <param name="underlyingBuilder">The underlying <see cref="IBuilder"/> to wrap.</param>
    /// <param name="prefix">The directory prefix to prepend to paths.</param>
    public PrefixBuilder(IBuilder underlyingBuilder, string prefix)
    {
        _prefix = prefix;
        _underlyingBuilder = underlyingBuilder;
    }

    /// <inheritdoc/>
    public void AddDirectory(string path)
        => _underlyingBuilder.AddDirectory(GetPath(path));

    /// <inheritdoc/>
    public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
        => _underlyingBuilder.AddFile(GetPath(path), stream, modifiedTime, executable);

    /// <inheritdoc/>
    public void AddHardlink(string path, string target, bool executable = false)
        => _underlyingBuilder.AddHardlink(GetPath(path), Path.Combine(_prefix, target), executable);

    /// <inheritdoc/>
    public void AddSymlink(string path, string target)
        => _underlyingBuilder.AddSymlink(GetPath(path), target);

    /// <inheritdoc/>
    public void Rename(string path, string target)
        => _underlyingBuilder.Rename(GetPath(path), Path.Combine(_prefix, target));

    /// <inheritdoc/>
    public void Remove(string path)
        => _underlyingBuilder.Remove(GetPath(path));

    /// <inheritdoc />
    public void MarkAsExecutable(string path)
        => _underlyingBuilder.MarkAsExecutable(GetPath(path));

    /// <inheritdoc />
    public void TurnIntoSymlink(string path)
        => _underlyingBuilder.TurnIntoSymlink(GetPath(path));

    /// <summary>
    /// Prepends the <see cref="_prefix"/> to a <paramref name="path"/>.
    /// </summary>
    /// <exception cref="IOException">The <see cref="_prefix"/> or the <paramref name="path"/> contain invalid characters.</exception>
    private string GetPath(string path)
    {
        try
        {
            return Path.Combine(_prefix, path);
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
