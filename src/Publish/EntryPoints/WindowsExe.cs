// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A native PE (Portable Executable) for Windows.
    /// </summary>
    public class WindowsExe : NativeExecutable, IIconContainer
    {
        /// <inheritdoc/>
        internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
        {
            if (!base.Analyze(baseDirectory, file)) return false;
            if (!StringUtils.EqualsIgnoreCase(file.Extension, @".exe")) return false;

            try
            {
                Parse(FileVersionInfo.GetVersionInfo(file.FullName));
                return Parse(new PEHeader(file.FullName));
            }
            #region Error handling
            catch (IOException)
            {
                return false;
            }
            #endregion
        }

        private void Parse(FileVersionInfo versionInfo)
        {
            Name = versionInfo.ProductName;
            Summary = string.IsNullOrEmpty(versionInfo.Comments) ? versionInfo.FileDescription : versionInfo.Comments;
            if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
            {
                try
                {
                    Version = new ImplementationVersion(versionInfo.ProductVersion.Trim());
                }
                catch (FormatException)
                {}
            }
        }

        protected virtual bool Parse([NotNull] PEHeader peHeader)
        {
            #region Sanity checks
            if (peHeader == null) throw new ArgumentNullException(nameof(peHeader));
            #endregion

            Architecture = new Architecture(OS.Windows, GetCpu(peHeader.FileHeader.Machine));
            if (peHeader.Subsystem >= Subsystem.WindowsCui) NeedsTerminal = true;
            return peHeader.Is32BitHeader
                ? (peHeader.OptionalHeader32.CLRRuntimeHeader.VirtualAddress == 0)
                : (peHeader.OptionalHeader64.CLRRuntimeHeader.VirtualAddress == 0);
        }

        [CLSCompliant(false)]
        protected static Cpu GetCpu(MachineType machine)
        {
            switch (machine)
            {
                case MachineType.I386:
                    return Cpu.All;
                case MachineType.X64:
                    return Cpu.X64;
                default:
                    return Cpu.Unknown;
            }
        }

#if !NETSTANDARD2_0
        /// <inheritdoc/>
        public System.Drawing.Icon ExtractIcon()
            => System.Drawing.Icon.ExtractAssociatedIcon(Path.Combine(BaseDirectory.FullName, RelativePath));
#endif
    }
}
