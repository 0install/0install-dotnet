// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Xml.Linq;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Client;

/// <summary>
/// Client for invoking Zero Install commands from within other applications.
/// </summary>
public class ZeroInstallClient : IZeroInstallClient
{
    private readonly IProcessLauncher _launcher;
    private readonly IProcessLauncher? _guiLauncher;

    /// <summary>
    /// Creates a new Zero Install client.
    /// </summary>
    /// <param name="launcher">Used to launch <c>0install</c> as a child process.</param>
    /// <param name="guiLauncher">Used to launch <c>0install-win</c> as a child process.</param>
    internal ZeroInstallClient(IProcessLauncher launcher, IProcessLauncher? guiLauncher = null)
    {
        _launcher = launcher;
        _guiLauncher = guiLauncher;
    }

    /// <summary>
    /// Creates a new Zero Install client.
    /// </summary>
    /// <param name="commandLine">The command-line used to launch <c>0install</c>. Whitespace must be properly escaped.</param>
    /// <param name="guiCommandLine">The optional command-line used to launch <c>0install-win</c>. Whitespace must be properly escaped.</param>
    public ZeroInstallClient(string commandLine, string? guiCommandLine = null)
        : this(
            launcher: new ZeroInstallLauncher(commandLine),
            guiLauncher: guiCommandLine?.To(x=> new ZeroInstallLauncher(x)))
    {}

    /// <summary>
    /// Creates a Zero Install client by detecting the location of <c>0install</c> using environment variables or the Windows registry.
    /// </summary>
    public static IZeroInstallClient Detect
        => new ZeroInstallClient(
            commandLine: ZeroInstallEnvironment.Cli ?? GetRegistryPath("0install") ?? "0install",
            guiCommandLine: ZeroInstallEnvironment.Gui ?? GetRegistryPath("0install-win"));

    private static string? GetRegistryPath(string executableName)
    {
        if (!WindowsUtils.IsWindows) return null;

        if (RegistryUtils.GetSoftwareString("Zero Install", "InstallLocation") is not {Length: >0} installLocation) return null;

        string path = Path.Combine(installLocation, $"{executableName}.exe");
        return File.Exists(path) ? path.EscapeArgument() : null;
    }

    /// <inheritdoc/>
    public void TrustKey(string fingerprint, string domain)
    {
        try
        {
            _launcher.Run("trust", "add", fingerprint, domain);
        }
        catch (NoChangesException)
        {}
    }

    /// <inheritdoc/>
    public async Task<Selections> SelectAsync(Requirements requirements, bool refresh = false, bool offline = false)
    {
        var args = new List<string> { "select", "--batch", "--xml" };
        if (refresh) args.Add("--refresh");
        if (offline) args.Add("--offline");
        args.Add(requirements.ToCommandLineArgs());

        string output = await Task.Run(() => _launcher.RunAndCapture(args.ToArray()));
        return XmlStorage.FromXmlString<Selections>(output);
    }

    /// <inheritdoc/>
    public async Task<Selections> DownloadAsync(Requirements requirements, bool refresh = false)
    {
        var args = new List<string> { "download", "--batch" };
        if (refresh) args.Add("--refresh");
        args.Add(requirements.ToCommandLineArgs());

        if (_guiLauncher == null)
        {
            args.Add("--xml");
            string output = await Task.Run(() => _launcher.RunAndCapture(args.ToArray()));
            return XmlStorage.FromXmlString<Selections>(output);
        }
        else
        {
            args.Add("--background");
            await Task.Run(() => _guiLauncher.Run(args.ToArray()));
            return await SelectAsync(requirements, offline: true);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(Requirements requirements)
    {
        var args = new List<string> { "update", "--batch", requirements.ToCommandLineArgs() };
        if (_guiLauncher != null) args.Add("--background");

        try
        {
            await Task.Run(() => (_guiLauncher ?? _launcher).Run(args.ToArray()));
        }
        catch (NoChangesException)
        {
            return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public void Run(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments)
    {
        var args = new List<string> { "run", "--no-wait" };
        if (refresh) args.Add("--refresh");
        args.Add(requirements.ToCommandLineArgs());
        args.Add(arguments);

        var launcher = needsTerminal ? _launcher : _guiLauncher ?? _launcher;
        launcher.Run(args.ToArray());
    }

    /// <inheritdoc/>
    public ProcessStartInfo GetRunStartInfo(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments)
    {
        var args = new List<string> { "run", "--batch" };
        if (refresh) args.Add("--refresh");
        args.Add(requirements.ToCommandLineArgs());
        args.Add(arguments);

        var launcher = needsTerminal ? _launcher : _guiLauncher ?? _launcher;
        return launcher.GetStartInfo(args.ToArray());
    }

    /// <inheritdoc/>
    public async Task<ISet<string>> GetIntegrationAsync(FeedUri uri, bool machineWide = false)
    {
        var args = new List<string> { "list-apps", "--batch", "--xml", uri.ToStringRfc() };
        if (machineWide) args.Add("--machine");
        string output = await Task.Run(() => _launcher.RunAndCapture(args.ToArray()));

        const string xmlNamespace = "http://0install.de/schema/desktop-integration/app-list";
        return new HashSet<string>(
            XElement.Parse(output)
                    .Descendants(XName.Get("app", xmlNamespace)).SingleOrDefault()
                   ?.Descendants(XName.Get("access-points", xmlNamespace)).SingleOrDefault()
                   ?.Descendants()
                    .Select(x => x.Name.LocalName)
         ?? Enumerable.Empty<string>());
    }

    /// <inheritdoc/>
    public async Task IntegrateAsync(FeedUri uri, IEnumerable<string>? add = null, IEnumerable<string>? remove = null, bool machineWide = false)
    {
        var args = new List<string> { "integrate", "--batch", uri.ToStringRfc() };
        if (machineWide) args.Add("--machine");

        void AddToArgs(string option, IEnumerable<string>? elements)
        {
            if (elements == null) return;
            foreach (string category in elements)
            {
                args.Add(option);
                args.Add(category);
            }
        }

        AddToArgs("--add", add);
        AddToArgs("--remove", remove);

        await Task.Run(() => _launcher.RunAndCapture(args.ToArray()));
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(FeedUri uri, bool machineWide = false)
    {
        var args = new List<string> { "remove", "--batch", uri.ToStringRfc() };
        if (machineWide) args.Add("--machine");

        await Task.Run(() => _launcher.RunAndCapture(args.ToArray()));
    }

    /// <inheritdoc/>
    public async Task FetchAsync(Implementation implementation)
        => await Task.Run(() => _launcher.RunAndCapture(
            writer => writer.WriteLineAsync(new Feed { Name = "Fetch", Elements = { implementation } }.ToXmlString().Replace("\n", "")),
            "fetch"));
}
