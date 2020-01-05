// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Uses an external process to solve <see cref="Requirements"/>.
    /// </summary>
    /// <remarks>The executable for external process is itself provided by another <see cref="ISolver"/>.</remarks>
    public class ExternalSolver : ISolver
    {
        #region Dependencies
        private readonly ISolver _backingSolver;
        private readonly ISelectionsManager _selectionsManager;
        private readonly IFetcher _fetcher;
        private readonly IExecutor _executor;
        private readonly IFeedManager _feedManager;
        private readonly ITaskHandler _handler;
        private readonly Requirements _solverRequirements;

        /// <summary>
        /// Creates a new external JSON solver.
        /// </summary>
        /// <param name="backingSolver">An internal solver used to find an implementation of the external solver.</param>
        /// <param name="selectionsManager">Used to check whether the external solver is already in the cache.</param>
        /// <param name="fetcher">Used to download implementations of the external solver.</param>
        /// <param name="executor">Used to launch the external solver.</param>
        /// <param name="externalSolverUri">The feed URI used to get the external solver.</param>
        /// <param name="feedManager">Provides access to remote and local <see cref="Feed"/>s. Handles downloading, signature verification and caching.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public ExternalSolver(ISolver backingSolver, ISelectionsManager selectionsManager, IFetcher fetcher, IExecutor executor, FeedUri externalSolverUri, IFeedManager feedManager, ITaskHandler handler)
        {
            _backingSolver = backingSolver ?? throw new ArgumentNullException(nameof(backingSolver));
            _selectionsManager = selectionsManager ?? throw new ArgumentNullException(nameof(selectionsManager));
            _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _feedManager = feedManager ?? throw new ArgumentNullException(nameof(feedManager));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _solverRequirements = new Requirements(externalSolverUri ?? throw new ArgumentNullException(nameof(externalSolverUri)));
        }
        #endregion

        /// <inheritdoc/>
        public Selections Solve(Requirements requirements)
        {
            #region Sanity checks
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (requirements.InterfaceUri == null) throw new ArgumentException(Resources.MissingInterfaceUri, nameof(requirements));
            #endregion

            Selections selections = default!;
            _handler.RunTask(new SimpleTask(Resources.ExternalSolverRunning, () =>
            {
                using var control = new ExternalSolverSession(GetStartInfo())
                {
                    {"confirm", args => DoConfirm((string)args[0])},
                    {"confirm-keys", args => DoConfirmKeys(new FeedUri((string)args[0]), args[1].ReparseAsJson<Dictionary<string, string[][]>>())},
                    {"update-key-info", args => null}
                };
                control.Invoke(args =>
                {
                    if ((string)args[0] == "ok")
                    {
                        _feedManager.Stale = args[1].ReparseAsJson(new {stale = false}).stale;
                        selections = XmlStorage.FromXmlString<Selections>((string)args[2]);
                    }
                    else throw new SolverException(((string)args[1]).Replace("\n", Environment.NewLine));
                }, "select", GetEffectiveRequirements(requirements), _feedManager.Refresh);
                while (selections == null)
                {
                    control.HandleStderr();
                    control.HandleNextChunk();
                }
                control.HandleStderr();
            }));

            // Invalidate in-memory feed cache, because external solver may have modified on-disk feed cache
            _feedManager.Clear();

            return selections;
        }

        private Selections? _solverSelections;

        private ProcessStartInfo GetStartInfo()
        {
            if (_solverSelections == null)
                _solverSelections = _backingSolver.Solve(_solverRequirements);

            var missing = _selectionsManager.GetUncachedImplementations(_solverSelections);
            _fetcher.Fetch(missing);

            var arguments = new[] {"--console", "slave", ExternalSolverSession.ApiVersion};
            for (int i = 0; i < (int)_handler.Verbosity; i++)
                arguments = arguments.Append("--verbose");
            var startInfo = _executor.Inject(_solverSelections)
                                     .AddArguments(arguments)
                                     .ToStartInfo();

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
        {
            var effectiveRequirements = requirements.Clone();
            effectiveRequirements.Command = requirements.Command ?? (requirements.Architecture.Cpu == Cpu.Source ? Command.NameCompile : Command.NameRun);
            return effectiveRequirements;
        }

        private string DoConfirm(string message) => _handler.Ask(message) ? "ok" : "cancel";

        private string DoConfirmKeys(FeedUri feedUri, Dictionary<string, string[][]> keys)
        {
            var key = keys.First();
            var hint = key.Value[0];

            string message = string.Format(Resources.AskKeyTrust, feedUri.ToStringRfc(), key.Key, hint[1], feedUri.Host);
            return _handler.Ask(message) ? "ok" : "cancel";
        }
    }
}
