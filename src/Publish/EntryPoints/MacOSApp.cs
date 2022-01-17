// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A binary inside a MacOS X application bundle.
/// </summary>
public sealed class MacOSApp : PosixExecutable
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        Debug.Assert(RelativePath != null);

        if (!RelativePath.GetLeftPartAtLastOccurrence('/').EndsWith(@".app/Contents/MacOS")) return false;

        // TODO: Parse MacOS plist
        Name = file.Name[..^file.Extension.Length];
        Architecture = new(OS.MacOSX, Cpu.All);
        return true;
    }
}
