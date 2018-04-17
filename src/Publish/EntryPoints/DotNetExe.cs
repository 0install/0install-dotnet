// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using JetBrains.Annotations;
using ZeroInstall.Publish.EntryPoints.Design;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

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
    public sealed class DotNetExe : WindowsExe
    {
        protected override bool Parse(PEHeader peHeader)
        {
            #region Sanity checks
            if (peHeader == null) throw new ArgumentNullException(nameof(peHeader));
            #endregion

            Architecture = new Architecture(OS.All, GetCpu(peHeader.FileHeader.Machine));
            if (peHeader.Subsystem >= Subsystem.WindowsCui) NeedsTerminal = true;
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
        [UsedImplicitly, CanBeNull]
        public ImplementationVersion MinimumRuntimeVersion { get; set; }

        /// <summary>
        /// The types of .NET runtimes supported by the application.
        /// </summary>
        [Category("Details (.NET)"), DisplayName(@".NET type"), Description("The types of .NET runtimes supported by the application.")]
        [DefaultValue(typeof(DotNetRuntimeType), "Any")]
        [UsedImplicitly]
        public DotNetRuntimeType RuntimeType { get; set; }

        /// <summary>
        /// Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!
        /// </summary>
        [Category("Details (.NET)"), DisplayName(@"External dependencies"), Description("Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!")]
        [DefaultValue(false)]
        [UsedImplicitly]
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
                            ? new FeedUri("http://repo.roscidus.com/dotnet/clr-monopath")
                            : new FeedUri("http://repo.roscidus.com/dotnet/clr");

                    case DotNetRuntimeType.MicrosoftOnlyClientProfile:
                        Architecture = new Architecture(OS.Windows, Architecture.Cpu);
                        return ExternalDependencies
                            ? new FeedUri("http://repo.roscidus.com/dotnet/clr-monopath")
                            : new FeedUri("http://repo.roscidus.com/dotnet/framework-client-profile");

                    case DotNetRuntimeType.MicrosoftOnlyFullProfile:
                        Architecture = new Architecture(OS.Windows, Architecture.Cpu);
                        return ExternalDependencies
                            ? new FeedUri("http://repo.roscidus.com/dotnet/clr-monopath")
                            : new FeedUri("http://repo.roscidus.com/dotnet/framework");

                    case DotNetRuntimeType.MonoOnly:
                        return new FeedUri("http://repo.roscidus.com/dotnet/mono");
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
                    Versions = MinimumRuntimeVersion
                }
            };
        }

        #region Equality
        private bool Equals(DotNetExe other)
            => base.Equals(other)
            && MinimumRuntimeVersion == other.MinimumRuntimeVersion
            && RuntimeType == other.RuntimeType
            && ExternalDependencies == other.ExternalDependencies;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is DotNetExe exe && Equals(exe);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (MinimumRuntimeVersion?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (int)RuntimeType;
                hashCode = (hashCode * 397) ^ ExternalDependencies.GetHashCode();
                return hashCode;
            }
        }
        #endregion
    }
}
