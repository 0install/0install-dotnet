// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Manages the contents of the <see cref="TrustDB"/>.
/// </summary>
public sealed class TrustMan : CliMultiCommand
{
    public const string Name = "trust";

    /// <inheritdoc/>
    public TrustMan(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    public override IEnumerable<string> SubCommandNames => new[] {Add.Name, Remove.Name, List.Name};

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
        public string ParentName => Name;

        protected TrustSubCommand(ICommandHandler handler)
            : base(handler)
        {}
    }

    public class Add : TrustSubCommand
    {
        public const string Name = "add";
        public override string Description => Resources.DescriptionTrustAdd;
        public override string Usage => "FINGERPRINT DOMAIN";
        protected override int AdditionalArgsMin => 2;
        protected override int AdditionalArgsMax => 2;

        public Add(ICommandHandler handler)
            : base(handler)
        {}

        /// <inheritdoc />
        public override ExitCode Execute()
        {
            var trustDB = TrustDB.Load();

            string fingerprint = AdditionalArgs[0];
            var domain = new Domain(AdditionalArgs[1]);

            if (trustDB.IsTrusted(fingerprint, domain))
                return ExitCode.NoChanges;
            trustDB.TrustKey(fingerprint, domain);

            trustDB.Save();
            return ExitCode.OK;
        }
    }

    public class Remove : TrustSubCommand
    {
        public const string Name = "remove";
        public override string Description => Resources.DescriptionTrustRemove;
        public override string Usage => "FINGERPRINT [DOMAIN]";
        protected override int AdditionalArgsMin => 1;
        protected override int AdditionalArgsMax => 2;

        public Remove(ICommandHandler handler)
            : base(handler)
        {}

        /// <inheritdoc />
        public override ExitCode Execute()
        {
            var trustDB = TrustDB.Load();

            bool found = AdditionalArgs.Count switch
            {
                1 => trustDB.UntrustKey(AdditionalArgs[0]),
                2 => trustDB.UntrustKey(AdditionalArgs[0], new(AdditionalArgs[1])),
                _ => throw new InvalidOperationException()
            };
            if (!found) return ExitCode.NoChanges;

            trustDB.Save();
            return ExitCode.OK;
        }
    }

    public class List : TrustSubCommand
    {
        public const string Name = "list";
        public override string Description => Resources.DescriptionTrustList;
        public override string Usage => "[FINGERPRINT]";
        protected override int AdditionalArgsMax => 1;

        public List(ICommandHandler handler)
            : base(handler)
        {}

        /// <inheritdoc />
        public override ExitCode Execute()
        {
            var trustDB = TrustDB.Load();

            switch (AdditionalArgs.Count)
            {
                case 0:
                    Handler.Output(Resources.TrustedKeys, trustDB.Keys);
                    return ExitCode.OK;

                case 1:
                    string fingerprint = AdditionalArgs[0];
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
