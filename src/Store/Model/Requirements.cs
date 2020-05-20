// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Values;
using Newtonsoft.Json;
using ZeroInstall.Store.Model.Design;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// A set of requirements/restrictions imposed by the user on the <see cref="Implementation"/> selection process. Used as input for the solver.
    /// </summary>
    [Serializable, XmlRoot("requirements", Namespace = Feed.XmlNamespace), XmlType("requirements", Namespace = Feed.XmlNamespace)]
    public class Requirements : ICloneable<Requirements>, IEquatable<Requirements>
    {
        /// <summary>
        /// The URI or local path (must be absolute) to the interface to solve the dependencies for.
        /// </summary>
        [Description("The URI or local path (must be absolute) to the interface to solve the dependencies for.")]
        [XmlIgnore, JsonProperty("interface")]
        public FeedUri InterfaceUri { get; set; }

        /// <summary>
        /// The name of the command in the implementation to execute. Will default to <see cref="Model.Command.NameRun"/> or <see cref="Model.Command.NameCompile"/> if <c>null</c>. Will not try to find any command if set to <see cref="string.Empty"/>.
        /// </summary>
        [Description("The name of the command in the implementation to execute. Will default to 'run' or 'compile' if null. Will not try to find any command if set to ''.")]
        [TypeConverter(typeof(CommandNameConverter))]
        [XmlAttribute("command"), JsonProperty("command")]
        public string? Command { get; set; }

        // Order is always alphabetical, duplicate entries are not allowed

        /// <summary>
        /// The preferred languages for the implementation.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Complete set can be replaced by PropertyGrid.")]
        [Description("The preferred languages for the implementation.")]
        [XmlIgnore, JsonIgnore]
        public LanguageSet Languages { get; private set; } = new LanguageSet();

        /// <summary>
        /// The architecture to find executables for. Find for the current system if left at default value.
        /// </summary>
        /// <remarks>Will default to <see cref="Model.Architecture.CurrentSystem"/> if left at default value. Will not try to find any command if set to <see cref="string.Empty"/>.</remarks>
        [Description("The architecture to find executables for. Find for the current system if left at default value.")]
        [XmlIgnore, JsonIgnore]
        public Architecture Architecture { get; set; }

        #region XML/JSON serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="InterfaceUri"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("interface"), JsonIgnore]
        public string InterfaceUriString { get => InterfaceUri?.ToStringRfc()!; set => InterfaceUri = new FeedUri(value); }

        /// <summary>Used for XML and JSON serialization.</summary>
        /// <seealso cref="Languages"/>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        [DefaultValue(""), JsonProperty("langs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string LanguagesString { get => Languages.ToString(); set => Languages = new LanguageSet(value); }

        /// <summary>Used for XML and JSON serialization.</summary>
        /// <seealso cref="Architecture"/>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue(false), XmlAttribute("source"), JsonProperty("source")]
        public bool Source
        {
            get => Architecture.Cpu == Cpu.Source;
            set
            {
                if (value) Architecture = new Architecture(Architecture.OS, Cpu.Source);
            }
        }

        /// <summary>Used for XML and JSON serialization.</summary>
        /// <seealso cref="Architecture"/>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue("*"), XmlAttribute("os"), JsonProperty("os", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string OSString { get => Architecture.OS.ConvertToString(); set => Architecture = new Architecture(value.ConvertFromString<OS>(), Architecture.Cpu); }

        /// <summary>Used for XML and JSON serialization.</summary>
        /// <seealso cref="Architecture"/>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue("*"), XmlAttribute("machine"), JsonProperty("cpu", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CpuString { get => Architecture.Cpu.ConvertToString(); set => Architecture = new Architecture(Architecture.OS, value.ConvertFromString<Cpu>()); }
        #endregion

        /// <summary>
        /// The ranges of versions of specific sub-implementations that can be chosen.
        /// </summary>
        [Description("The ranges of versions of specific sub-implementations that can be chosen.")]
        [XmlIgnore, JsonProperty("extra_restrictions")]
        public Dictionary<FeedUri, VersionRange> ExtraRestrictions { get; } = new Dictionary<FeedUri, VersionRange>();

        /// <summary>
        /// Adds version restriction for a specific feeds. Merges with any existing restrictions for that feed.
        /// </summary>
        /// <param name="feedUri">The feed URI to apply the restriction for.</param>
        /// <param name="versions">The version range set to restrict to.</param>
        public void AddRestriction(FeedUri feedUri, VersionRange versions)
            => ExtraRestrictions[feedUri] = ExtraRestrictions.TryGetValue(feedUri, out var existingVersions)
                ? existingVersions.Intersect(versions)
                : versions;

        // Order is not important (but is preserved), duplicate entries are not allowed (but not enforced)

        /// <summary>
        /// Specifies that the selected implementations must be from one of the given distributions (e.g. Debian, RPM).
        /// The special value <see cref="Restriction.DistributionZeroInstall"/> may be used to require implementations provided by Zero Install (i.e. one not provided by a <see cref="PackageImplementation"/>).
        /// </summary>
        /// <remarks>Used internally by solvers, copied from <see cref="Restriction.Distributions"/>, not set directly by user, not serialized.</remarks>
        [Browsable(false)]
        [XmlIgnore, JsonIgnore]
        public List<string> Distributions { get; } = new List<string>();

        /// <summary>
        /// Creates an empty requirements object. Use this to fill in values incrementally, e.g. when parsing command-line arguments.
        /// </summary>
        public Requirements() {}

        /// <summary>
        /// Creates a new requirements object.
        /// </summary>
        /// <param name="interfaceUri">The URI or local path (must be absolute) to the interface to solve the dependencies for.</param>
        /// <param name="command">The name of the command in the implementation to execute. Will default to <see cref="Model.Command.NameRun"/> or <see cref="Model.Command.NameCompile"/> if <c>null</c>. Will not try to find any command if set to <see cref="string.Empty"/>.</param>
        /// <param name="architecture">The architecture to find executables for. Find for the current system if left at default value.</param>
        public Requirements(FeedUri interfaceUri, string? command = null, Architecture architecture = default)
        {
            InterfaceUri = interfaceUri;
            Command = command;
            Architecture = architecture;
        }

        /// <summary>
        /// Creates a new requirements object.
        /// </summary>
        /// <param name="interfaceUri">The URI or local path (must be absolute) to the interface to solve the dependencies for. Must be an HTTP(S) URL or an absolute local path.</param>
        /// <exception cref="UriFormatException"><paramref name="interfaceUri"/> is not a valid HTTP(S) URL or an absolute local path.</exception>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Justification = "Convenience overload that internally calls the Uri version")]
        public Requirements(string interfaceUri)
            : this(new FeedUri(interfaceUri))
        {}

        /// <summary>
        /// Creates a new requirements object.
        /// </summary>
        /// <param name="interfaceUri">The URI or local path (must be absolute) to the interface to solve the dependencies for. Must be an HTTP(S) URL or an absolute local path.</param>
        /// <param name="command">The name of the command in the implementation to execute. Will default to <see cref="Model.Command.NameRun"/> or <see cref="Model.Command.NameCompile"/> if <c>null</c>. Will not try to find any command if set to <see cref="string.Empty"/>.</param>
        /// <param name="architecture">The architecture to find executables for. Find for the current system if left at default value.</param>
        /// <exception cref="UriFormatException"><paramref name="interfaceUri"/> is not a valid HTTP(S) URL or an absolute local path.</exception>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Justification = "Convenience overload that internally calls the Uri version")]
        public Requirements(string interfaceUri, string? command = null, Architecture architecture = default)
            : this(new FeedUri(interfaceUri), command, architecture)
        {}

        /// <summary>
        /// Substitutes blank values with default values appropriate for the current system.
        /// </summary>
        public Requirements ForCurrentSystem()
        {
            var cloned = Clone();

            cloned.Command = Command ?? (Architecture.Cpu == Cpu.Source ? Model.Command.NameCompile : Model.Command.NameRun);
            cloned.Architecture = new Architecture(
                (Architecture.OS == OS.All) ? Architecture.CurrentSystem.OS : Architecture.OS,
                (Architecture.Cpu == Cpu.All) ? Architecture.CurrentSystem.Cpu : Architecture.Cpu);
            if (Languages.Count == 0) cloned.Languages.Add(CultureInfo.CurrentUICulture);

            return cloned;
        }

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Requirements"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Requirements"/>.</returns>
        public Requirements Clone()
        {
            var requirements = new Requirements(InterfaceUri, Command, Architecture);
            requirements.Languages.AddRange(Languages);
            requirements.ExtraRestrictions.AddRange(ExtraRestrictions);
            requirements.Distributions.AddRange(Distributions);
            return requirements;
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the requirements in the form "InterfaceUri (Command)". Not safe for parsing!
        /// </summary>
        public override string ToString()
            => string.IsNullOrEmpty(Command)
                ? InterfaceUri.ToStringRfc() ?? ""
                : InterfaceUri.ToStringRfc() + " (" + Command + ")";

        /// <summary>
        /// Transforms the requirements into a command-line arguments.
        /// </summary>
        public string[] ToCommandLineArgs()
        {
            var args = new List<string>();

            if (Command != null) args.AddRange(new[] {"--command", Command});
            if (Architecture.Cpu == Cpu.Source) args.Add("--source");
            else
            {
                if (Architecture.OS != OS.All) args.AddRange(new[] {"--os", Architecture.OS.ConvertToString()});
                if (Architecture.Cpu != Cpu.All) args.AddRange(new[] {"--cpu", Architecture.Cpu.ConvertToString()});
            }
            foreach (var language in Languages)
                args.AddRange(new[] {"--language", language.ToString()});
            foreach (var (uri, range) in ExtraRestrictions)
                args.AddRange(new[] {"--version-for", uri.ToStringRfc(), range.ToString()});
            args.Add(InterfaceUri.ToStringRfc());

            return args.ToArray();
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Requirements other)
            => other != null
            && InterfaceUri == other.InterfaceUri
            && Command == other.Command
            && Architecture == other.Architecture
            && Languages.SetEquals(other.Languages)
            && ExtraRestrictions.UnsequencedEquals(other.ExtraRestrictions)
            && Distributions.UnsequencedEquals(other.Distributions);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj.GetType() == typeof(Requirements) && Equals((Requirements)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                InterfaceUri,
                Command,
                Architecture,
                Languages.GetUnsequencedHashCode(),
                ExtraRestrictions.GetUnsequencedHashCode(),
                Distributions.GetUnsequencedHashCode());
        #endregion
    }
}
