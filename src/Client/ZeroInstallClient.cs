// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Client
{
    /// <summary>
    /// Client for invoking Zero Install commands from within other applications.
    /// </summary>
    public class ZeroInstallClient : IZeroInstallClient
    {
        private readonly ISubProcess _subProcess;
        private readonly IProcessLauncher _launcher;
        private readonly IProcessLauncher? _guiLauncher;

        /// <summary>
        /// Creates a new Zero Install client.
        /// </summary>
        /// <param name="subProcess">Used to launch <c>0install</c>, captures its output and waits until it has terminated.</param>
        /// <param name="launcher">Used to launch <c>0install</c> as ane external process.</param>
        /// <param name="guiLauncher">Used to launch <c>0install-win</c> as ane external process.</param>
        internal ZeroInstallClient(ISubProcess subProcess, IProcessLauncher launcher, IProcessLauncher? guiLauncher = null)
        {
            _subProcess = subProcess;
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
                subProcess: new SubProcess(ProcessUtils.FromCommandLine(commandLine)),
                launcher: new ProcessLauncher(commandLine),
                guiLauncher: guiCommandLine?.To(x=> new ProcessLauncher(x)))
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

            string? installLocation = RegistryUtils.GetSoftwareString("Zero Install", "InstallLocation");
            if (string.IsNullOrEmpty(installLocation)) return null;

            string path = Path.Combine(installLocation, $"{executableName}.exe");
            return File.Exists(path) ? path.EscapeArgument() : null;
        }

        /// <inheritdoc/>
        public async Task<Selections> SelectAsync(Requirements requirements, bool refresh = false, bool offline = false)
        {
            var args = new List<string> { "select", "--batch", "--xml" };
            if (refresh) args.Add("--refresh");
            if (offline) args.Add("--offline");
            args.AddRange(requirements.ToCommandLineArgs());

            string output = await Task.Run(() => _subProcess.Run(args.ToArray()));
            return XmlStorage.FromXmlString<Selections>(output);
        }

        /// <inheritdoc/>
        public async Task<Selections> DownloadAsync(Requirements requirements, bool refresh = false)
        {
            var args = new List<string> { "download", "--batch" };
            if (refresh) args.Add("--refresh");
            args.AddRange(requirements.ToCommandLineArgs());

            if (_guiLauncher == null)
            {
                args.Add("--xml");
                string output = await Task.Run(() => _subProcess.Run(args.ToArray()));
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
        public void Run(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments)
        {
            var args = new List<string> { "run" };
            if (refresh) args.Add("--refresh");
            args.Add("--no-wait");
            args.AddRange(requirements.ToCommandLineArgs());
            args.AddRange(arguments);

            if (needsTerminal || _guiLauncher == null)
                _launcher.Start(args.ToArray());
            else
                _guiLauncher.Start(args.ToArray());
        }

        /// <inheritdoc/>
        public Process RunWithProcess(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments)
        {
            var args = new List<string> { "run" };
            if (refresh) args.Add("--refresh");
            args.AddRange(requirements.ToCommandLineArgs());
            args.AddRange(arguments);

            return needsTerminal || _guiLauncher == null
                ? _launcher.Start(args.ToArray())
                : _guiLauncher.Start(args.ToArray());
        }

        /// <inheritdoc/>
        public async Task<ISet<string>> GetIntegrationAsync(FeedUri uri)
        {
            string output = await Task.Run(() => _subProcess.Run("list-apps", "--batch", "--xml", uri.ToStringRfc()));

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
        public async Task IntegrateAsync(FeedUri uri, IEnumerable<string>? add = null, IEnumerable<string>? remove = null)
        {
            var args = new List<string> { "integrate", "--batch", uri.ToStringRfc() };

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

            await Task.Run(() => _subProcess.Run(args.ToArray()));
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(FeedUri uri)
            => await Task.Run(() => _subProcess.Run("remove", "--batch", uri.ToStringRfc()));

        /// <inheritdoc/>
        public async Task FetchAsync(Implementation implementation)
            => await Task.Run(() => _subProcess.Run(
                writer => writer.WriteLineAsync(new Feed { Elements = { implementation } }.ToXmlString().Replace("\n", "")),
                "fetch"));
    }
}
