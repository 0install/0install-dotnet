// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A Windows batch file/script.
/// </summary>
public sealed class WindowsBatch : NativeExecutable
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        if (!StringUtils.EqualsIgnoreCase(file.Extension, @".bat") && !StringUtils.EqualsIgnoreCase(file.Extension, @".cmd")) return false;

        Architecture = new(OS.Windows, Cpu.All);
        Name = file.Name[..^file.Extension.Length];
        NeedsTerminal = true;
        return true;
    }
}
