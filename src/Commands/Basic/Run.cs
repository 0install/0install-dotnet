// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Services.Executors;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// This behaves similarly to <see cref="Download"/>, except that it also runs the program after ensuring it is in the cache.
/// </summary>
#if NETFRAMEWORK
[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
#endif
public class Run : Download
{
    public new const string Name = "run";
    public override string Description => Resources.DescriptionRun;
    public override string Usage => $"{base.Usage} [ARGS]";
    protected override int AdditionalArgsMax => int.MaxValue;

    /// <summary>>An alternative executable to to run from the main <see cref="Implementation"/> instead of <see cref="Element.Main"/>.</summary>
    private string? _overrideMain;

    /// <summary>Instead of executing the selected program directly, pass it as an argument to this program.</summary>
    private string? _wrapper;

    /// <summary>Immediately returns once the chosen program has been launched instead of waiting for it to finish executing.</summary>
    private bool _noWait;

    /// <summary>
    /// Creates a new run command.
    /// </summary>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    public Run(ICommandHandler handler)
        : base(handler, outputOptions: false)
    {
        Options.Add("m|main=", () => Resources.OptionMain, newMain => _overrideMain = newMain);
        Options.Add("w|wrapper=", () => Resources.OptionWrapper, newWrapper => _wrapper = newWrapper);
        Options.Add("no-wait", () => Resources.OptionNoWait, _ => _noWait = true);

        // Work-around to disable interspersed arguments (needed for passing arguments through to sub-processes)
        Options.Add("<>", value =>
        {
            AdditionalArgs.Add(value);

            // Stop using options parser, treat everything from here on as unknown
            Options.Clear();
        });
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        if (Requirements.Command == "") throw new OptionException(Resources.NoRunWithEmptyCommand, "command");

        Solve();

        DownloadUncachedImplementations();

        Handler.CancellationToken.ThrowIfCancellationRequested();
        Handler.DisableUI();

        var builder = Executor.Inject(Selections, _overrideMain)
                              .AddWrapper(_wrapper)
                              .AddArguments(AdditionalArgs.ToArray())
                              .SetEnvironmentVariable(ZeroInstallEnvironment.FeedUriName, Requirements.InterfaceUri.ToStringRfc())
                              .SetCallbackEnvironmentVariables();

        using var process = TryStart(builder);
        if (process == null)
        {
            Log.Warn("No process launched");
            return ExitCode.OK;
        }

        // Wait for launched program to become visible before closing 0install window (for a smoother visual transition)
        WaitForGui(process);
        Handler.CloseUI();

        BackgroundUpdate();
        BackgroundSelfUpdate();

        if (_noWait)
        {
            Log.Debug("Not waiting for program to exit");
            return WindowsUtils.IsWindows ? (ExitCode)process.Id : ExitCode.OK;
        }
        else
        {
            Log.Debug("Waiting for program to exit");
            return (ExitCode)process.WaitForExitCode();
        }
    }

    private static Process? TryStart(IEnvironmentBuilder builder)
    {
        try
        {
            return builder.Start();
        }
        #region Error handling
        catch (FileNotFoundException ex)
        {
            if (ZeroInstallInstance.IsLibraryMode && ProgramUtils.GuiStartInfo(StoreMan.Name, StoreMan.Audit.Name) is {} storeAudit)
            {
                Log.Info("Automatically starting store audit because of missing file in Kiosk mode", ex);
                storeAudit.Start();
                return null;
            }

            // Wrap exception to add context information
            throw new FileNotFoundException(string.Format(Resources.TryHelp, "0install store audit"), ex);
        }
        #endregion
    }

    private void WaitForGui(Process process)
    {
        if (WindowsUtils.IsWindows
         && Handler is {IsGui: true, Background: false}
         && Selections != null
         && FeedCache.GetFeed(Selections.InterfaceUri) is {NeedsTerminal: false})
        {
            try
            {
                process.WaitForInputIdle(milliseconds: 5000);
            }
            catch (InvalidOperationException)
            {}
        }
    }

    /// <inheritdoc/>
#pragma warning disable 8776
    [MemberNotNull(nameof(Selections))]
#pragma warning restore 8776
    protected override void Solve()
    {
        if (Config.NetworkUse > NetworkLevel.Minimal && !FeedManager.Refresh)
        {
            Log.Info("Minimal-network Solve for faster startup");
            using (PropertyPointer.For(() => Config.NetworkUse).SetTemp(NetworkLevel.Minimal))
                base.Solve();
        }
        else base.Solve();
    }

    /// <summary>
    /// Updates the application in a background process.
    /// </summary>
    private void BackgroundUpdate()
    {
        if (FeedManager.ShouldRefresh
         && !FeedManager.RateLimit(Requirements.InterfaceUri))
        {
            Log.Info("Starting background update because feeds have become stale");
            StartCommandBackground(Update.Name, ["--batch", ..Requirements.ToCommandLineArgs()]);
        }
    }
}
