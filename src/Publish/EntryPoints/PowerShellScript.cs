/*
 * Copyright 2010-2017 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
        {
            get
            {
                switch (PowerShellType)
                {
                    case PowerShellType.Any:
                    default:
                        return new FeedUri("http://repo.roscidus.com/powershell/powershell");

                    case PowerShellType.WindowsOnly:
                        return new FeedUri("http://repo.roscidus.com/powershell/windows");

                    case PowerShellType.CoreOnly:
                        return new FeedUri("http://repo.roscidus.com/powershell/core");
                }
            }
        }

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
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)PowerShellType;
                return hashCode;
            }
        }
        #endregion
    }
}
