// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common;
using NDesk.Options;
using ZeroInstall.Commands.Basic;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Create an alias for a <see cref="Run"/> command.
    /// </summary>
    public sealed class AddAlias : AppCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public new const string Name = "alias";

        /// <summary>The alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName = "add-alias";

        /// <inheritdoc/>
        public override string Usage => "ALIAS [INTERFACE [COMMAND]]";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionAddAlias;

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 3;
        #endregion

        #region State
        private bool _resolve;
        private bool _remove;

        /// <inheritdoc/>
        public AddAlias(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);

            Options.Add("resolve", () => Resources.OptionAliasResolve, _ => _resolve = true);
            Options.Add("remove", () => Resources.OptionAliasRemove, _ => _remove = true);
        }
        #endregion

        /// <inheritdoc />
        protected override ExitCode ExecuteHelper()
        {
            string aliasName = AdditionalArgs[0];

            if (_resolve || _remove)
            {
                if (AdditionalArgs.Count > 1) throw new OptionException(Resources.TooManyArguments + Environment.NewLine + AdditionalArgs[1].EscapeArgument(), null);

                var appAlias = IntegrationManager.AppList.GetAppAlias(aliasName, out var appEntry);
                if (appAlias == null)
                {
                    Handler.Output(Resources.AppAlias, string.Format(Resources.AliasNotFound, aliasName));
                    return ExitCode.InvalidArguments;
                }

                if (_resolve)
                {
                    string result = appEntry.InterfaceUri.ToStringRfc();
                    if (!string.IsNullOrEmpty(appAlias.Command)) result += Environment.NewLine + "Command: " + appAlias.Command;
                    Handler.OutputLow(Resources.AppAlias, result);
                }
                if (_remove)
                {
                    IntegrationManager.RemoveAccessPoints(appEntry, new AccessPoint[] {appAlias});

                    Handler.OutputLow(Resources.AppAlias, string.Format(Resources.AliasRemoved, aliasName, appEntry.Name));
                }
                return ExitCode.OK;
            }
            else
            {
                if (AdditionalArgs.Count < 2 || string.IsNullOrEmpty(AdditionalArgs[1])) throw new OptionException(Resources.MissingArguments, null);
                string command = (AdditionalArgs.Count >= 3) ? AdditionalArgs[2] : null;

                var appEntry = GetAppEntry(IntegrationManager, ref InterfaceUri);
                CreateAlias(appEntry, aliasName, command);
                return ExitCode.OK;
            }
        }
    }
}
