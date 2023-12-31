// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Values.Design;

namespace ZeroInstall.Publish.EntryPoints;

[Equatable]
public partial class DotNetExe : WindowsExe
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;

        // Actual app is the .dll, not the .exe
        if (!File.Exists($"{file.FullName[..^4]}.dll")) return false;

        try
        {
            var runtimeOptions = JsonStorage.LoadJson<RuntimeConfig>($"{file.FullName[..^4]}.runtimeconfig.json").RuntimeOptions;
            var frameworks = runtimeOptions?.Frameworks ?? [runtimeOptions?.Framework ?? new Framework(null, null)];

            RuntimeVersion = new(frameworks.FirstOrDefault(x => x.Name != null && x.Name.EndsWith(".App"))?.Version ?? "1.0.0");
            NeedsAspNetCore = frameworks.Any(x => x.Name == "Microsoft.AspNetCore.App");
            NeedsTerminal = frameworks.All(x => x.Name != "Microsoft.WindowsDesktop.App");
        }
        catch
        {
            return false;
        }

        return true;
    }

    private record RuntimeConfig(RuntimeOptions? RuntimeOptions);
    private record RuntimeOptions(Framework? Framework, List<Framework>? Frameworks);
    private record Framework(string? Name, string? Version);

    protected override bool Parse(PEHeader peHeader) => true;

    /// <summary>
    /// The version of the .NET Runtime required by the application.
    /// </summary>
    [Category("Details (.NET)"), DisplayName(".NET Runtime version"), Description("The version of the .NET Runtime required by the application.")]
    [DefaultValue("")]
    [TypeConverter(typeof(StringConstructorConverter<ImplementationVersion>))]
    public ImplementationVersion RuntimeVersion { get; set; } = new("1.0.0");

    /// <summary>
    /// Indicates whether the app needs the ASP.NET Core Runtime.
    /// </summary>
    [Category("Details (.NET)"), DisplayName("Needs ASP.NET Core Runtime"), Description("Indicates whether the app needs the ASP.NET Core Runtime.")]
    [DefaultValue(false)]
    public bool NeedsAspNetCore { get; set; }

    /// <inheritdoc/>
    public override Command CreateCommand()
        => NeedsTerminal
            ? new()
            {
                Name = CommandName,
                Path = $"{RelativePath![..^4]}.dll",
                Runner = new()
                {
                    InterfaceUri = new(NeedsAspNetCore ? "https://apps.0install.net/dotnet/apsnetcore-runtime.xml" : "https://apps.0install.net/dotnet/runtime.xml"),
                    Versions = GetRuntimeVersionRange()
                }
            }
            : new()
            {
                Name = CommandName,
                Path = RelativePath,
                Dependencies =
                {
                    new()
                    {
                        InterfaceUri = new("https://apps.0install.net/dotnet/windowsdesktop-runtime.xml"),
                        Versions = GetRuntimeVersionRange()
                    }
                }
            };

    private VersionRange GetRuntimeVersionRange()
    {
        var decimals = RuntimeVersion.FirstPart.Decimals?.Count > 2 ? RuntimeVersion.FirstPart.Decimals : new[] {5L, 0L};
        return new Constraint
        {
            NotBefore = RuntimeVersion,
            Before = new($"{decimals[0]}.{decimals[1] + 1}")
        };
    }
}
