// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Xml;
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
            commandLine: ZeroInstallEnvironment.Cli
                      ?? GetFromRegistry("0install", machineWide: false)
                      ?? GetFromRegistry("0install", machineWide: true)
                      ?? "0install",
            guiCommandLine: ZeroInstallEnvironment.Gui
                         ?? GetFromRegistry("0install-win", machineWide: false)
                         ?? GetFromRegistry("0install-win", machineWide: true));

    private static string? GetFromRegistry(string executableName, bool machineWide)
        => ZeroInstallDeployment.GetPath(machineWide) is {} dirPath
            ? Path.Combine(dirPath, $"{executableName}.exe").EscapeArgument()
            : null;

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

        return XmlStorage.FromXmlString<Selections>(
            await Task.Run(() => _launcher.RunAndCapture(args.ToArray())));
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
            return XmlStorage.FromXmlString<Selections>(
                await Task.Run(() => _launcher.RunAndCapture(args.ToArray())));
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

        GetLauncher(needsTerminal)
           .Run(args.ToArray());
    }

    /// <inheritdoc/>
    public ProcessStartInfo GetRunStartInfo(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments)
    {
        var args = new List<string> { "run", "--batch" };
        if (refresh) args.Add("--refresh");
        args.Add(requirements.ToCommandLineArgs());
        args.Add(arguments);

        return GetLauncher(needsTerminal)
           .GetStartInfo(args.ToArray());
    }

    private IProcessLauncher GetLauncher(bool needsTerminal)
        => needsTerminal ? _launcher : _guiLauncher ?? _launcher;

    /// <inheritdoc/>
    public ISet<string> GetIntegration(FeedUri uri, bool machineWide = false)
    {
        var args = new List<string> { "list-apps", "--batch", "--xml", uri.ToStringRfc() };
        if (machineWide) args.Add("--machine");

        const string xmlNamespace = "http://0install.de/schema/desktop-integration/app-list";
        try
        {
            return new HashSet<string>(
                XElement.Parse(_launcher.RunAndCapture(args.ToArray()))
                        .Descendants(XName.Get("app", xmlNamespace)).SingleOrDefault()
                       ?.Descendants(XName.Get("access-points", xmlNamespace)).SingleOrDefault()
                       ?.Descendants()
                        .Select(x => x.Name.LocalName)
             ?? Enumerable.Empty<string>());
        }
        #region Error handling
        catch (XmlException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException("Failed to parse 0install response", ex);
        }
        #endregion
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
    public void Remove(FeedUri uri, bool machineWide = false)
    {
        var args = new List<string> { "remove", "--batch", uri.ToStringRfc() };
        if (machineWide) args.Add("--machine");

        _launcher.RunAndCapture(args.ToArray());
    }

    /// <inheritdoc/>
    public async Task FetchAsync(Implementation implementation)
        => await Task.Run(() => _launcher.RunAndCapture(
            writer => writer.WriteLineAsync(new Feed { Name = "Fetch", Elements = { implementation } }.ToXmlString().Replace("\n", "")),
            "fetch"));
}
