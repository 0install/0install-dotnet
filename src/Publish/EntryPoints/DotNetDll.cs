// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using Generator.Equals;

namespace ZeroInstall.Publish.EntryPoints;

[Equatable]
public sealed partial class DotNetDll : DotNetExe
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;

        // Prefer extraction information from .exe instead of .dll, because it provides more details.
        if (File.Exists(file.FullName[..^4] + ".exe")) return false;

        // GUI applications can only be started via .exe
        NeedsTerminal = true;

        return true;
    }

    protected override string ExecutableExtension => @".dll";
}
