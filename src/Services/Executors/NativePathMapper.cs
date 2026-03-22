// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Executors;

/// <summary>
/// A path mapper that performs no translation (identity mapping).
/// Used for native process execution where paths don't need translation.
/// </summary>
public class NativePathMapper : IPathMapper
{
    /// <inheritdoc/>
    public string MapPath(string hostPath) => hostPath;

    /// <inheritdoc/>
    public string UnmapPath(string targetPath) => targetPath;
}
