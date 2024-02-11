// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Uses an external process to solve <see cref="Requirements"/>.
/// The executable for external process is itself provided by another <see cref="ISolver"/>.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class ExternalSolver(
    ISolver backingSolver,
    ISelectionsManager selectionsManager,
    IFetcher fetcher,
    IExecutor executor,
    IFeedManager feedManager,
    ITaskHandler handler,
    Requirements solverRequirements) : ISolver
{
    /// <inheritdoc/>
    public Selections Solve(Requirements requirements)
    {
        #region Sanity checks
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (requirements.InterfaceUri == null) throw new ArgumentException(Resources.MissingInterfaceUri, nameof(requirements));
        #endregion

        Log.Info(Resources.ExternalSolverRunning);

        using var control = new ExternalSolverSession(GetStartInfo())
        {
            {"confirm", args => DoConfirm((string)args[0])},
            {"confirm-keys", args => DoConfirmKeys(new((string)args[0]), args[1].ReparseAsJson<Dictionary<string, string[][]>>())},
            {"update-key-info", _ => null}
        };

        Selections? selections = null;
        control.Invoke(args =>
        {
            if ((string)args[0] == "ok")
            {
                feedManager.Stale = args[1].ReparseAsJson(new {stale = false}).stale;
                selections = XmlStorage.FromXmlString<Selections>((string)args[2]);
            }
            else throw new SolverException(((string)args[1]).Replace("\n", Environment.NewLine));
        }, "select", GetEffectiveRequirements(requirements), false /*_feedManager.Refresh*/); // Pretend refresh is always false to avoid downloading feeds in external process (could cause problems with HTTPS and GPG validation)
        while (selections == null)
        {
            control.HandleStderr();
            control.HandleNextChunk();
        }
        control.HandleStderr();

        // Invalidate in-memory feed cache, because external solver may have modified on-disk feed cache
        feedManager.Clear();

        return selections!;
    }

    private Selections? _solverSelections;

    private ProcessStartInfo GetStartInfo()
    {
        _solverSelections ??= backingSolver.Solve(solverRequirements);

        foreach (var implementation in selectionsManager.GetUncachedImplementations(_solverSelections))
            fetcher.Fetch(implementation);

        var builder = executor.Inject(_solverSelections)
                               .AddArguments("--console", "slave", ExternalSolverSession.ApiVersion);
        for (int i = 0; i < (int)handler.Verbosity; i++)
            builder.AddArguments("--verbose");

        var startInfo = builder.ToStartInfo();
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.EnvironmentVariables["GNUPGHOME"] = OpenPgp.VerifyingHomeDir;
        if (Locations.IsPortable)
            startInfo.EnvironmentVariables["ZEROINSTALL_PORTABLE_BASE"] = Locations.PortableBase;
        return startInfo;
    }

    private static Requirements GetEffectiveRequirements(Requirements requirements)
        => requirements with
        {
            Command = requirements.Command ?? (requirements.Architecture.Cpu == Cpu.Source ? Command.NameCompile : Command.NameRun)
        };

    private string DoConfirm(string message) => handler.Ask(message) ? "ok" : "cancel";

    private string DoConfirmKeys(FeedUri feedUri, Dictionary<string, string[][]> keys)
    {
        var key = keys.First();
        var hint = key.Value[0];

        string message = string.Format(Resources.AskKeyTrust, feedUri.ToStringRfc(), key.Key, hint[1], feedUri.Host);
        return handler.Ask(message) ? "ok" : "cancel";
    }
}
