// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Store;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// View or change <see cref="Config"/>.
    /// </summary>
    public class Configure : CliCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "config";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionConfig;

        /// <inheritdoc/>
        public override string Usage => "[NAME [VALUE|default]]";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 2;
        #endregion

        #region State
        /// <inheritdoc/>
        public Configure(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("tab=", () => Resources.OptionConfigTab, (ConfigTab tab) => Config.InitialTab = tab);
        }
        #endregion

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            switch (AdditionalArgs.Count)
            {
                case 0:
                    Handler.Output(Resources.Configuration, Config);
                    break;

                case 1:
                    GetOptions(AdditionalArgs[0]);
                    break;

                case 2:
                    SetOption(AdditionalArgs[0], AdditionalArgs[1]);
                    Config.Save();
                    break;
            }

            return ExitCode.OK;
        }

        private void GetOptions(string key)
        {
            try
            {
                Handler.Output(key, Config.GetOption(key));
            }
            #region Error handling
            catch (KeyNotFoundException)
            {
                throw new OptionException(string.Format(Resources.InvalidArgument, key), key);
            }
            #endregion
        }

        private void SetOption(string key, string value)
        {
            try
            {
                if (value == "default") Config.ResetOption(key);
                else Config.SetOption(key, value);
            }
            #region Error handling
            catch (KeyNotFoundException)
            {
                throw new OptionException(string.Format(Resources.InvalidArgument, key), key);
            }
            catch (FormatException ex)
            {
                throw new OptionException(ex.Message, key);
            }
            #endregion
        }
    }
}
