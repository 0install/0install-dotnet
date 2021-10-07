// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common;
using NanoByte.Common.Storage;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// List all current <see cref="AppEntry"/>s in the <see cref="AppList"/>.
    /// </summary>
    public class ListApps : IntegrationCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "list-apps";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionListApps;

        /// <inheritdoc/>
        public override string Usage => "[PATTERN]";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 1;
        #endregion

        /// <summary>Indicates the user wants a machine-readable output.</summary>
        private bool _xmlOutput;

        /// <inheritdoc/>
        public ListApps(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("xml", () => Resources.OptionXml, _ => _xmlOutput = true);
        }

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            var apps = AppList.LoadSafe(MachineWide);

            // Apply filter
            if (AdditionalArgs.Count > 0) apps.Entries.RemoveAll(x => !x.Name.ContainsIgnoreCase(AdditionalArgs[0]));

            if (_xmlOutput) Handler.Output(Resources.MyApps, apps.ToXmlString());
            else Handler.Output(Resources.MyApps, apps.Entries);
            return ExitCode.OK;
        }
    }
}
