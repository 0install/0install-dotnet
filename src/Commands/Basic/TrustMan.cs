// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Model.Trust;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Manages the contents of the <see cref="TrustDB"/>.
/// </summary>
public sealed class TrustMan(ICommandHandler handler) : CliMultiCommand(handler)
{
    public const string Name = "trust";

    /// <inheritdoc/>
    public override IEnumerable<string> SubCommandNames => [Add.Name, Remove.Name, List.Name];

    /// <inheritdoc/>
    public override CliCommand GetCommand(string commandName)
        => (commandName ?? throw new ArgumentNullException(nameof(commandName))) switch
        {
            Add.Name => new Add(Handler),
            Remove.Name => new Remove(Handler),
            List.Name => new List(Handler),
            _ => throw new OptionException(string.Format(Resources.UnknownCommand, commandName), commandName)
        };

    public abstract class TrustSubCommand : CliCommand, ICliSubCommand
    {
        protected bool MachineWide;

        protected TrustSubCommand(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("m|machine", () => Resources.OptionMachine, _ => MachineWide = true);
        }

        public string ParentName => Name;

        protected TrustDB Load()
            => MachineWide ? TrustDB.LoadMachineWide() : TrustDB.Load();
    }

    public class Add(ICommandHandler handler) : TrustSubCommand(handler)
    {
        public const string Name = "add";
        public override string Description => Resources.DescriptionTrustAdd;
        public override string Usage => "FINGERPRINT DOMAIN";
        protected override int AdditionalArgsMin => 2;
        protected override int AdditionalArgsMax => 2;

        /// <inheritdoc />
        public override ExitCode Execute()
        {
            if (MachineWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator)
                throw new NotAdminException(Resources.MustBeAdminForMachineWide);

            var trustDB = Load();

            string fingerprint = AdditionalArgs[0];
            var domain = new Domain(AdditionalArgs[1]);

            if (trustDB.IsTrusted(fingerprint, domain))
                return ExitCode.NoChanges;
            trustDB.TrustKey(fingerprint, domain);

            trustDB.Save();
            return ExitCode.OK;
        }
    }

    public class Remove(ICommandHandler handler) : TrustSubCommand(handler)
    {
        public const string Name = "remove";
        public override string Description => Resources.DescriptionTrustRemove;
        public override string Usage => "FINGERPRINT [DOMAIN]";
        protected override int AdditionalArgsMin => 1;
        protected override int AdditionalArgsMax => 2;

        /// <inheritdoc />
        public override ExitCode Execute()
        {
            if (MachineWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator)
                throw new NotAdminException(Resources.MustBeAdminForMachineWide);

            var trustDB = Load();

            bool found = AdditionalArgs switch
            {
                [var fingerprint] => trustDB.UntrustKey(fingerprint),
                [var fingerprint, var domain] => trustDB.UntrustKey(fingerprint, new(domain)),
                _ => throw new InvalidOperationException()
            };
            if (!found) return ExitCode.NoChanges;

            trustDB.Save();
            return ExitCode.OK;
        }
    }

    public class List(ICommandHandler handler) : TrustSubCommand(handler)
    {
        public const string Name = "list";
        public override string Description => Resources.DescriptionTrustList;
        public override string Usage => "[FINGERPRINT]";
        protected override int AdditionalArgsMax => 1;

        /// <inheritdoc />
        public override ExitCode Execute()
        {
            var trustDB = Load();

            switch (AdditionalArgs)
            {
                case []:
                    if (Handler.IsGui) ShowConfig(ConfigTab.Trust);
                    else Handler.Output(Resources.TrustedKeys, trustDB.Keys);
                    return ExitCode.OK;

                case [var fingerprint]:
                    Handler.Output(
                        string.Format(Resources.TrustedForDomains, fingerprint),
                        trustDB.Keys.FirstOrDefault(x => x.Fingerprint == fingerprint)?.Domains ?? new());
                    return ExitCode.OK;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
