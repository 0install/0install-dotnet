// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
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

            Architecture = new Architecture(OS.Windows, Cpu.All);
            Name = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
            NeedsTerminal = true;
            return true;
        }
    }
}
