// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NDesk.Options;
using ZeroInstall.Commands.Basic;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Services.Solvers;

namespace ZeroInstall.Commands.Desktop
{
    partial class Self
    {
        /// <summary>
        /// Updates Zero Install itself to the most recent version.
        /// </summary>
        public class Update : Run, ICliSubCommand
        {
            #region Metadata
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public new const string Name = "update";

            public const string TopLevelName = "self-update";

            public string ParentName => Self.Name;

            /// <inheritdoc/>
            public override string Description => Resources.DescriptionSelfUpdate;

            /// <inheritdoc/>
            public override string Usage => "[OPTIONS]";
            #endregion

            #region State
            /// <summary>Perform the update even if the currently installed version is the same or newer.</summary>
            private bool _force;

            /// <summary>Restart the <see cref="Central"/> GUI after the update.</summary>
            private bool _restartCentral;

            /// <inheritdoc/>
            public Update(ICommandHandler handler)
                : base(handler)
            {
                NoWait = true;
                FeedManager.Refresh = true;

                //Options.Remove("no-wait");
                //Options.Remove("refresh");

                Options.Add("force", () => Resources.OptionForceSelfUpdate, _ => _force = true);
                Options.Add("restart-central", () => Resources.OptionRestartCentral, _ => _restartCentral = true);
            }
            #endregion

            /// <inheritdoc/>
            public override void Parse(IEnumerable<string> args)
            {
                // NOTE: Does not call base method

                AdditionalArgs.AddRange(Options.Parse(args));
                if (AdditionalArgs.Count != 0) throw new OptionException(Resources.TooManyArguments + Environment.NewLine + AdditionalArgs.JoinEscapeArguments(), null);

                SetInterfaceUri(Config.SelfUpdateUri ?? throw new UriFormatException(Resources.SelfUpdateDisabled));
                if (ProgramUtils.GuiAssemblyName != null) Requirements.Command = Command.NameRunGui;

                // Instruct new version of Zero Install in the cache to deploy itself over the location of the current version
                AdditionalArgs.AddRange(new[] {Self.AltName, Deploy.Name, "--batch", Locations.InstallBase});

                if (_restartCentral) AdditionalArgs.Add("--restart-central");
            }

            /// <inheritdoc/>
            public override ExitCode Execute()
            {
                if (ZeroInstallInstance.IsRunningFromCache) throw new NotSupportedException(Resources.SelfUpdateBlocked);

                try
                {
                    Solve();
                }
                #region Error handling
                catch (WebException ex) when (Handler.Background)
                {
                    Log.Info("Suppressed network-related error message due to background mode");
                    Log.Info(ex);
                    return ExitCode.WebError;
                }
                catch (SolverException ex) when (Handler.Background)
                {
                    Log.Info("Suppressed Solver-related error message due to background mode");
                    Log.Info(ex);
                    return ExitCode.SolverError;
                }
                #endregion

                if (UpdateFound())
                {
                    DownloadUncachedImplementations();

                    Handler.CancellationToken.ThrowIfCancellationRequested();
                    if (!Handler.Ask(string.Format(Resources.SelfUpdateAvailable, Selections.MainImplementation.Version), defaultAnswer: true))
                        throw new OperationCanceledException();

                    LaunchImplementation();
                    return ExitCode.OK;
                }
                else return ExitCode.OK;
            }

            private bool UpdateFound() => _force || (Selections.MainImplementation.Version > ImplementationVersion.ZeroInstall);
        }
    }
}
