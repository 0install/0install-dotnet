// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Any file with the POSIX executable bit (xbit) set.
/// </summary>
public abstract class PosixExecutable : NativeExecutable
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        return IsExecutable(file.FullName);
    }
}
