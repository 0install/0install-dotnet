// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Services;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Bootstrap;

/// <summary>
/// Represents the process of downloading and running an instance of Zero Install. Handles command-line arguments.
/// </summary>
public sealed partial class BootstrapProcess : ServiceProvider
{
    private readonly IBootstrapHandler _handler;

    /// <summary>
    /// Executes the bootstrap process controlled by command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to handle.</param>
    /// <returns>The exit status code to end the process with.</returns>
    public ExitCode Execute(IEnumerable<string> args)
    {
        _userArgs.Add(_options.Parse(args));
        if (_machineWide && !OSUtils.IsAdmin) throw new NotAdminException();

        // May switch to machine-wide mode, so this needs to happen before handling config
        if (BootstrapConfig.Instance.CustomizableStorePath && !_prepareOffline) CustomizeStorePath();

        LoadConfig();
        SaveConfig();
        TrustKeys();

        try
        {
            ImportEmbedded();
            ImportDirectory();
        }
        #region Error handling
        catch (ObjectDisposedException ex)
        {
            Log.Warn("Stream closed while importing content", ex);
            return ExitCode.IOError;
        }
        #endregion

        if (_appUri == null) ApplySharedOptions();
        if (_offline) Config.NetworkUse = NetworkLevel.Offline;

        if (_prepareOffline) return ExecuteExport();
        var exitCode = ExecuteIntegrate();
        return exitCode == ExitCode.OK ? ExecuteRun() : exitCode;
    }

    /// <summary>
    /// Runs <c>0install export</c>.
    /// </summary>
    private ExitCode ExecuteExport()
    {
        var args = new List<string> {"export", "--refresh"};

        if (_appUri != null)
        {
            args.Add(_appUri.ToStringRfc());
            if (_appVersion != null) args.Add($"--version={_appVersion}");
            args.Add("--include-zero-install");
        }
        else
        {
            args.Add(SelfUpdateUri.ToStringRfc());
            if (_version != null) args.Add($"--version={_version}");
        }
        args.Add(Locations.InstallBase);

        return SwitchToZeroInstall(args);
    }

    /// <summary>
    /// Runs <c>0install integrate</c> if requested by <see cref="BootstrapConfig"/>.
    /// </summary>
    private ExitCode ExecuteIntegrate()
    {
        if (_appUri != null && BootstrapConfig.Instance.IntegrateArgs is {} integrateArgs && !_noIntegrate)
        {
            var args = new List<string> {"integrate", _appUri.ToStringRfc(), "--no-download"};
            if (_machineWide) args.Add("--machine");
            args.Add(ProcessUtils.SplitArguments(_integrateArgs ?? integrateArgs));

            if (ZeroInstallDeployment.FindOther(_machineWide) is {} deployedInstance)
                EnsureRequestedZeroInstallVersion(deployedInstance);

            var startInfo = GetStartInfo(args);
            return _handler.RunTaskAndReturn(ResultTask.Create(
                $"Integrating {BootstrapConfig.Instance.AppName}",
                () => (ExitCode)startInfo.Run()));
        }

        return ExitCode.OK;
    }

    /// <summary>
    /// Checks if a deployed instance of Zero Install matches the requested <see cref="_version"/> and updates it if not.
    /// </summary>
    private void EnsureRequestedZeroInstallVersion(string path)
    {
        if (_version == null) return;

        string versionFile = Path.Combine(path, "0install.exe");
        var deployedVersion = ImplementationVersion.FromSemVer(FileVersionInfo.GetVersionInfo(versionFile).ProductVersion!);
        if (!_version.Match(deployedVersion))
        {
            Log.Info($"Zero Install instance deployed at {path} has version {deployedVersion} that does not match desired version {_version}");
            var startInfo = ZeroInstallCached(IsLibraryMode(path)
                ? ["self", "deploy", "--batch", "--library", path]
                : ["self", "deploy", "--batch", path]);
            _handler.RunTask(new ActionTask("Updating Zero Install", () => startInfo.Run()));
        }
    }

    /// <summary>
    /// Runs <c>0install</c> with <c>run</c>, <c>central</c> or other user-provided arguments.
    /// </summary>
    private ExitCode ExecuteRun()
    {
        var args = new List<string>();

        if (_appUri != null)
        {
            args.Add(_noRun ? "download" : "run");
            if (_appVersion != null) args.AddRange(["--version", _appVersion.ToString()]);
            if (!_noRun && _handler.IsGui && !_wait) args.Add("--no-wait");
            args.Add(_appUri.ToStringRfc());
            if (!_noRun) args.Add(ProcessUtils.SplitArguments(BootstrapConfig.Instance.AppArgs));
        }

        args.Add(_userArgs);

        if (args is [])
        {
            if (_handler.IsGui) args.Add("central");
            else
            {
                _handler.Output("Help", HelpText);
                return ExitCode.UserCanceled;
            }
        }

        return SwitchToZeroInstall(args);
    }
}
