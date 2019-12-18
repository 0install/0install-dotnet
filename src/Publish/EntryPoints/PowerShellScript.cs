// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    public enum PowerShellType
    {
        Any,
        WindowsOnly,
        CoreOnly
    }

    /// <summary>
    /// A script written in PowerShell.
    /// </summary>
    public sealed class PowerShellScript : InterpretedScript
    {
        /// <inheritdoc/>
        internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
            => base.Analyze(baseDirectory, file) && StringUtils.EqualsIgnoreCase(file.Extension, @".ps1");

        public override Command CreateCommand() => new Command
        {
            Name = CommandName,
            Path = RelativePath,
            Runner = new Runner
            {
                InterfaceUri = InterpreterInterface,
                Versions = InterpreterVersions,
                Arguments = {"-ExecutionPolicy", "Bypass"}
            }
        };

        /// <inheritdoc/>
        protected override FeedUri InterpreterInterface
            => PowerShellType switch
            {
                PowerShellType.WindowsOnly => new FeedUri("https://apps.0install.net/powershell/windows.xml"),
                PowerShellType.CoreOnly => new FeedUri("https://apps.0install.net/powershell/core.xml"),
                _ => new FeedUri("https://apps.0install.net/powershell/powershell.xml"),
            };

        /// <summary>
        /// The types of PowerShell supported by the script.
        /// </summary>
        [Category("Details (Script)"), DisplayName(@"PowerShell type"), Description("The types of PowerShell supported by the script.")]
        [DefaultValue(typeof(PowerShellType), "Any")]
        [UsedImplicitly]
        public PowerShellType PowerShellType { get; set; }

        #region Equality
        private bool Equals(PowerShellScript other)
            => base.Equals(other)
            && PowerShellType == other.PowerShellType;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is PowerShellScript script && Equals(script);
        }

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), PowerShellType);
        #endregion
    }
}
