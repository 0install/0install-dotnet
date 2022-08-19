// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A shebang (#!) script for execution on a POSIX-style operating system.
/// </summary>
public sealed class PosixScript : PosixExecutable
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;

        string? firstLine = file.ReadFirstLine(Encoding.ASCII);
        if (string.IsNullOrEmpty(firstLine) || !firstLine.StartsWith(@"#!")) return false;

        Architecture = new(OS.Posix);
        Name = file.Name;
        NeedsTerminal = true;
        return true;
    }
}
