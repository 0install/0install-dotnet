// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using Generator.Equals;
using ZeroInstall.Model;
using ZeroInstall.Publish.EntryPoints.Design;

namespace ZeroInstall.Publish.EntryPoints
{
    public enum DotNetRuntimeType
    {
        Any,
        MicrosoftOnlyClientProfile,
        MicrosoftOnlyFullProfile,
        MonoOnly
    }

    /// <summary>
    /// A .NET/Mono executable.
    /// </summary>
    [Equatable]
    public sealed partial class DotNetExe : WindowsExe
    {
        protected override bool Parse(PEHeader peHeader)
        {
            #region Sanity checks
            if (peHeader == null) throw new ArgumentNullException(nameof(peHeader));
            #endregion

            Architecture = new(OS.Windows, GetCpu(peHeader.FileHeader.Machine));
            if (peHeader.Subsystem >= PESubsystem.WindowsCui) NeedsTerminal = true;
            return peHeader.Is32BitHeader
                ? (peHeader.OptionalHeader32.CLRRuntimeHeader.VirtualAddress != 0)
                : (peHeader.OptionalHeader64.CLRRuntimeHeader.VirtualAddress != 0);
        }

        /// <summary>
        /// The minimum version of the .NET Runtime required by the application.
        /// </summary>
        [Category("Details (.NET)"), DisplayName(@"Minimum .NET version"), Description("The minimum version of the .NET Runtime required by the application.")]
        [DefaultValue("")]
        [TypeConverter(typeof(DotNetVersionConverter))]
        public ImplementationVersion? MinimumRuntimeVersion { get; set; }

        /// <summary>
        /// The types of .NET Runtime supported by the application.
        /// </summary>
        [Category("Details (.NET)"), DisplayName(@".NET type"), Description("The types of .NET runtimes supported by the application.")]
        [DefaultValue(typeof(DotNetRuntimeType), "Any")]
        public DotNetRuntimeType RuntimeType { get; set; }

        /// <summary>
        /// Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!
        /// </summary>
        [Category("Details (.NET)"), DisplayName(@"External dependencies"), Description("Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!")]
        [DefaultValue(false)]
        public bool ExternalDependencies { get; set; }

        /// <inheritdoc/>
        public override Command CreateCommand()
        {
            FeedUri GetInterfaceUri()
            {
                switch (RuntimeType)
                {
                    case DotNetRuntimeType.Any:
                    default:
                        return ExternalDependencies
                            ? new("https://apps.0install.net/dotnet/clr-monopath.xml")
                            : new("https://apps.0install.net/dotnet/clr.xml");

                    case DotNetRuntimeType.MicrosoftOnlyClientProfile:
                        Architecture = new(OS.Windows, Architecture.Cpu);
                        return ExternalDependencies
                            ? new("https://apps.0install.net/dotnet/clr-monopath.xml")
                            : new("https://apps.0install.net/dotnet/framework-client-profile.xml");

                    case DotNetRuntimeType.MicrosoftOnlyFullProfile:
                        Architecture = new(OS.Windows, Architecture.Cpu);
                        return ExternalDependencies
                            ? new("https://apps.0install.net/dotnet/clr-monopath.xml")
                            : new("https://apps.0install.net/dotnet/framework.xml");

                    case DotNetRuntimeType.MonoOnly:
                        return new("https://apps.0install.net/dotnet/mono.xml");
                }
            }

            return new Command
            {
                Name = CommandName,
                Path = RelativePath,
                Runner = new Runner
                {
                    InterfaceUri = GetInterfaceUri(),
                    Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                    Versions = ToVersionRange(MinimumRuntimeVersion)
                }
            };
        }
    }
}
