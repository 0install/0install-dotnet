/*
 * Copyright 2010-2016 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Uses an external process controlled via a IPC to solve requirements. The external process is itself provided by another <see cref="ISolver"/>.
    /// </summary>
    public class ExternalSolver : ISolver
    {
        private const string ApiVersion = "2.7";

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
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="feedManager">Provides access to remote and local <see cref="Feed"/>s. Handles downloading, signature verification and caching.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public ExternalSolver([NotNull] ISolver backingSolver, [NotNull] ISelectionsManager selectionsManager, [NotNull] IFetcher fetcher, [NotNull] IExecutor executor, [NotNull] Config config, [NotNull] IFeedManager feedManager, [NotNull] ITaskHandler handler)
        {
            _backingSolver = backingSolver ?? throw new ArgumentNullException(nameof(backingSolver));
            _selectionsManager = selectionsManager ?? throw new ArgumentNullException(nameof(selectionsManager));
            _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _feedManager = feedManager ?? throw new ArgumentNullException(nameof(feedManager));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));

            if (config == null) throw new ArgumentNullException(nameof(config));
            _solverRequirements = new Requirements(config.ExternalSolverUri);
        }
        #endregion

        /// <inheritdoc/>
        public Selections Solve(Requirements requirements)
        {
            #region Sanity checks
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (requirements.InterfaceUri == null) throw new ArgumentException(Resources.MissingInterfaceUri, nameof(requirements));
            #endregion

            Selections selections = null;
            _handler.RunTask(new SimpleTask(Resources.ExternalSolverRunning, () =>
            {
                using (var control = new JsonControl(GetStartInfo())
                {
                    {"confirm", args => DoConfirm((string)args[0])},
                    {"confirm-keys", args => DoConfirmKeys(new FeedUri((string)args[0]), args[1].ReparseAsJson<Dictionary<string, string[][]>>())},
                    {"update-key-info", args => null}
                })
                {
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
                }

                // Hacky workaround for sporadic race conditions (files created by external process not immediately visible to parent)
                Thread.Sleep(1000);
            }));

            return selections;
        }

        private Selections _solverSelections;

        private ProcessStartInfo GetStartInfo()
        {
            if (_solverSelections == null)
                _solverSelections = _backingSolver.Solve(_solverRequirements);

            var missing = _selectionsManager.GetUncachedImplementations(_solverSelections);
            _fetcher.Fetch(missing);

            var arguments = new[] {"--console", "slave", ApiVersion};
            for (int i = 0; i < (int)_handler.Verbosity; i++)
                arguments = arguments.Append("--verbose");
            var startInfo = _executor
                .Inject(_solverSelections)
                .AddArguments(arguments)
                .ToStartInfo();

            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

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

        /// <summary>
        /// Controls communication with an external process via JSON interface.
        /// </summary>
        private sealed class JsonControl : Dictionary<string, Func<object[], object>>, IDisposable
        {
            private readonly Stream _stdin;
            private readonly Stream _stdout;
            private readonly StreamConsumer _stderr;

            public JsonControl(ProcessStartInfo startInfo)
            {
                var process = startInfo.Start();
                Debug.Assert(process != null);

                _stdin = process.StandardInput.BaseStream;
                _stdout = process.StandardOutput.BaseStream;
                _stderr = new StreamConsumer(process.StandardError);

                var apiNotification = GetJsonChunk();
                if (apiNotification == null ||
                    apiNotification[0].ToString() != "invoke" ||
                    apiNotification[1] != null ||
                    apiNotification[2].ToString() != "set-api-version")
                    throw new IOException("External solver did not respond correctly to handshake.");

                var apiVersion = new ImplementationVersion(apiNotification[3].ReparseAsJson<string[]>()[0]);
                if (apiVersion >= new ImplementationVersion(ApiVersion))
                    Log.Debug("Agreed on 0install slave API version " + apiVersion);
                else throw new IOException("Failed to agree on slave API version. External solver insisted on: " + apiVersion);
            }

            [CanBeNull]
            private object[] GetJsonChunk()
            {
                var chunk = GetChunk();
                if (chunk == null) return null;

                string json = Encoding.UTF8.GetString(chunk);
                return JsonStorage.FromJsonString<object[]>(json);
            }

            [CanBeNull]
            private byte[] GetChunk()
            {
                var preamble = _stdout.Read(11, throwOnEnd: false);
                if (preamble == null) return null;

                int length = Convert.ToInt32(Encoding.UTF8.GetString(preamble).TrimEnd('\n'), 16);
                return _stdout.Read(length);
            }

            private void SendJsonChunk([NotNull] params object[] value)
            {
                string json = value.ToJsonString();
                SendChunk(Encoding.UTF8.GetBytes(json));
            }

            private void SendChunk(byte[] data)
            {
                _stdin.Write(Encoding.UTF8.GetBytes($"0x{data.Length:x8}\n"));
                _stdin.Write(data);
                _stdin.Flush();
            }

            private int _nextTicket;
            private readonly Dictionary<string, Action<object[]>> _callbacks = new Dictionary<string, Action<object[]>>();

            public void Invoke(Action<object[]> onSuccess, string operation, params object[] args)
            {
                string ticket = _nextTicket++.ToString();
                _callbacks[ticket] = onSuccess;

                SendJsonChunk("invoke", ticket, operation, args);
            }

            public void HandleStderr()
            {
                string message;
                while ((message = _stderr.ReadLine()) != null)
                {
                    if (message.StartsWith("error: ")) Log.Error("External solver: " + message.Substring("error: ".Length));
                    else if (message.StartsWith("warning: ")) Log.Warn("External solver: " + message.Substring("warning: ".Length));
                    else if (message.StartsWith("info: ")) Log.Info("External solver: " + message.Substring("info: ".Length));
                    else if (message.StartsWith("debug: ")) Log.Debug("External solver: " + message.Substring("debug: ".Length));
                    else Log.Debug("External solver: " + message);
                }
            }

            public void HandleNextChunk()
            {
                var apiRequest = GetJsonChunk();
                if (apiRequest == null) throw new IOException("External solver exited unexpectedly.");

                string type = (string)apiRequest[0];
                string ticket = (string)apiRequest[1];
                string operation = (string)apiRequest[2];
                var args = apiRequest[3];

                switch (type)
                {
                    case "invoke":
                        try
                        {
                            var response = this[operation](args.ReparseAsJson<object[]>());
                            ReplyOK(ticket, response);
                        }
                        catch (Exception ex)
                        {
                            ReplyFail(ticket, ex.Message);
                            throw;
                        }
                        break;

                    case "return":
                        switch (operation)
                        {
                            case "ok":
                                _callbacks[ticket](args.ReparseAsJson<object[]>());
                                break;
                            case "ok+xml":
                                // ReSharper disable once AssignNullToNotNullAttribute
                                string xml = Encoding.UTF8.GetString(GetChunk());
                                Log.Debug("XML from external solver: " + xml);
                                _callbacks[ticket](args.ReparseAsJson<object[]>().Append(xml));
                                break;
                            case "fail":
                                throw new IOException(((string)args).Replace("\n", Environment.NewLine));
                        }
                        break;
                }
            }

            private void ReplyOK(string ticket, object response) => SendJsonChunk("return", ticket, "ok", new[] {response});

            private void ReplyFail(string ticket, string message) => SendJsonChunk("return", ticket, "fail", message);

            public void Dispose()
            {
                _stdin.Close();
                _stdout.Close();
            }
        }
    }
}
