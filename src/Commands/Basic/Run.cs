// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// This behaves similarly to <see cref="Download"/>, except that it also runs the program after ensuring it is in the cache.
    /// </summary>
    public class Run : Download
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public new const string Name = "run";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionRun;

        /// <inheritdoc/>
        public override string Usage => base.Usage + " [ARGS]";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => int.MaxValue;
        #endregion

        #region State
        /// <summary>>An alternative executable to to run from the main <see cref="Implementation"/> instead of <see cref="Element.Main"/>.</summary>
        private string? _overrideMain;

        /// <summary>Instead of executing the selected program directly, pass it as an argument to this program.</summary>
        private string? _wrapper;

        /// <summary>Immediately returns once the chosen program has been launched instead of waiting for it to finish executing.</summary>
        private bool _noWait;

        /// <summary>
        /// Creates a new run command.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
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
        #endregion

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            if (Requirements.Command == "") throw new OptionException(Resources.NoRunWithEmptyCommand, "command");

            Solve();

            DownloadUncachedImplementations();

            Handler.CancellationToken.ThrowIfCancellationRequested();
            Handler.DisableUI();

            var process = Executor.Inject(Selections!, _overrideMain)
                                  .AddWrapper(_wrapper)
                                  .AddArguments(AdditionalArgs.ToArray())
                                  .Start();

            Handler.CloseUI();

            BackgroundUpdate();
            SelfUpdateCheck();

            if (process == null) return ExitCode.OK;
            if (_noWait) return (WindowsUtils.IsWindows ? (ExitCode)process.Id : ExitCode.OK);
            else
            {
                Log.Debug("Waiting for application to exit");
                process.WaitForExit();
                return (ExitCode)process.ExitCode;
            }
        }

        /// <inheritdoc/>
        protected override void Solve()
        {
            if (Config.NetworkUse == NetworkLevel.Full && !FeedManager.Refresh)
            {
                Log.Info("Minimal-network Solve for faster startup");
                Config.NetworkUse = NetworkLevel.Minimal;

                try
                {
                    base.Solve();
                }
                finally
                {
                    // Restore original configuration
                    Config.NetworkUse = NetworkLevel.Full;
                }
            }
            else base.Solve();
        }

        /// <summary>
        /// Updates the application in a background process.
        /// </summary>
        private void BackgroundUpdate()
        {
            if (!FeedManager.ShouldRefresh || !NetUtils.IsInternetConnected) return;

            // Prevent multiple concurrent updates
            if (FeedManager.RateLimit(Requirements.InterfaceUri)) return;

            Log.Info("Starting background update because feeds have become stale");
            StartCommandBackground(Update.Name, Requirements.ToCommandLineArgs().Prepend("--batch"));
        }
    }
}
