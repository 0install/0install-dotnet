// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Values;
using Newtonsoft.Json;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model;

/// <summary>
/// A set of requirements/restrictions imposed by the user on the <see cref="Implementation"/> selection process. Used as input for the solver.
/// </summary>
[Serializable, XmlRoot("requirements", Namespace = Feed.XmlNamespace), XmlType("requirements", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Requirements : ICloneable<Requirements>
{
    /// <summary>
    /// The URI or local path (must be absolute) to the interface to solve the dependencies for.
    /// </summary>
    [Description("The URI or local path (must be absolute) to the interface to solve the dependencies for.")]
    [XmlIgnore, JsonProperty("interface")]
    public FeedUri InterfaceUri { get; set; } = default!;

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
    [SetEquality]
    public LanguageSet Languages { get; private set; } = new();

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
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    [XmlAttribute("interface"), JsonIgnore]
    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public string InterfaceUriString { get => InterfaceUri?.ToStringRfc()!; set => InterfaceUri = new(value); }

    /// <summary>Used for XML and JSON serialization.</summary>
    /// <seealso cref="Languages"/>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
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
            if (value) Architecture = new(Architecture.OS, Cpu.Source);
        }
    }

    /// <summary>Used for XML and JSON serialization.</summary>
    /// <seealso cref="Architecture"/>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [DefaultValue("*"), XmlAttribute("os"), JsonProperty("os", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string OSString { get => Architecture.OS.ConvertToString(); set => Architecture = new(value.ConvertFromString<OS>(), Architecture.Cpu); }

    /// <summary>Used for XML and JSON serialization.</summary>
    /// <seealso cref="Architecture"/>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [DefaultValue("*"), XmlAttribute("machine"), JsonProperty("cpu", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string CpuString { get => Architecture.Cpu.ConvertToString(); set => Architecture = new(Architecture.OS, value.ConvertFromString<Cpu>()); }
    #endregion

    /// <summary>
    /// The ranges of versions of specific sub-implementations that can be chosen.
    /// </summary>
    [Description("The ranges of versions of specific sub-implementations that can be chosen.")]
    [XmlIgnore, JsonProperty("extra_restrictions")]
    [UnorderedEquality]
    public Dictionary<FeedUri, VersionRange> ExtraRestrictions { get; } = new();

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
    [UnorderedEquality]
    public List<string> Distributions { get; } = new();

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
    /// Convenience cast for creating simple <see cref="Requirements"/> from a <see cref="FeedUri"/>.
    /// </summary>
    public static implicit operator Requirements(FeedUri uri) => new(uri);

    /// <summary>
    /// Substitutes blank values with default values appropriate for the current system.
    /// </summary>
    public Requirements ForCurrentSystem()
    {
        var cloned = Clone();

        cloned.Command ??= Architecture.Cpu == Cpu.Source ? Model.Command.NameCompile : Model.Command.NameRun;
        cloned.Architecture = new(
            Architecture.OS == OS.All ? Architecture.CurrentSystem.OS : Architecture.OS,
            Architecture.Cpu == Cpu.All ? Architecture.CurrentSystem.Cpu : Architecture.Cpu);
        if (Languages.Count == 0) cloned.Languages.Add(CultureInfo.CurrentUICulture);

        return cloned;
    }

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Requirements"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Requirements"/>.</returns>
    public Requirements Clone() => new(InterfaceUri, Command, Architecture)
    {
        Languages = {Languages},
        ExtraRestrictions = {ExtraRestrictions},
        Distributions = {Distributions}
    };
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the requirements in the form "InterfaceUri (Command)". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => string.IsNullOrEmpty(Command)
            ? InterfaceUriString
            : InterfaceUriString + " (" + Command + ")";

    /// <summary>
    /// Transforms the requirements into a command-line arguments.
    /// </summary>
    public string[] ToCommandLineArgs()
    {
        var args = new List<string>();

        if (Command != null) args.Add(new[] {"--command", Command});
        if (Architecture.Cpu == Cpu.Source) args.Add("--source");
        else
        {
            if (Architecture.OS != OS.All) args.Add(new[] {"--os", Architecture.OS.ConvertToString()});
            if (Architecture.Cpu != Cpu.All) args.Add(new[] {"--cpu", Architecture.Cpu.ConvertToString()});
        }
        foreach (var language in Languages)
            args.Add(new[] {"--language", language.ToString()});
        foreach (var (uri, range) in ExtraRestrictions)
            args.Add(new[] {"--version-for", uri.ToStringRfc(), range.ToString()});
        args.Add(InterfaceUri.ToStringRfc());

        return args.ToArray();
    }
    #endregion
}
