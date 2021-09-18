// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.IO;
using Generator.Equals;
using NanoByte.Common;
using NanoByte.Common.Values.Design;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A .NET Core application.
    /// </summary>
    [Equatable]
    public sealed partial class DotNetCoreApp : Candidate
    {
        /// <inheritdoc />
        internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
            => base.Analyze(baseDirectory, file)
            && StringUtils.EqualsIgnoreCase(file.Extension, @".dll")
            && File.Exists(file.FullName[..^file.Extension.Length] + @"deps.json");

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
                InterfaceUri = new("https://apps.0install.net/dotnet/core.xml"),
                Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                Versions = ToVersionRange(MinimumRuntimeVersion)
            }
        };
    }
}
