// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Values.Design;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A .NET Core application.
    /// </summary>
    public sealed class DotNetCoreApp : Candidate
    {
        /// <inheritdoc />
        internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
            => base.Analyze(baseDirectory, file)
            && StringUtils.EqualsIgnoreCase(file.Extension, @".dll")
            && File.Exists(file.FullName.Substring(file.FullName.Length - file.Extension.Length) + @"deps.json");

        /// <summary>
        /// The minimum version of the .NET Core Runtime required by the application.
        /// </summary>
        [Category("Details (.NET Core)"), DisplayName(@"Minimum .NET Core version"), Description("The minimum version of the .NET Core Runtime required by the application.")]
        [DefaultValue("")]
        [TypeConverter(typeof(StringConstructorConverter<ImplementationVersion>))]
        public ImplementationVersion? MinimumRuntimeVersion { get; set; } = new("2.0");

        /// <inheritdoc/>
        public override Command CreateCommand() => new()
        {
            Name = CommandName,
            Path = RelativePath,
            Runner = new Runner
            {
                InterfaceUri = new FeedUri("https://apps.0install.net/dotnet/core.xml"),
                Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                Versions = new Constraint {NotBefore = MinimumRuntimeVersion}
            }
        };

        #region Equality
        private bool Equals(DotNetCoreApp other)
            => base.Equals(other)
            && MinimumRuntimeVersion == other.MinimumRuntimeVersion;

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is DotNetCoreApp exe && Equals(exe);
        }

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), MinimumRuntimeVersion);
        #endregion
    }
}
