// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// Manages the contents of the <see cref="IImplementationStore"/>s.
    /// </summary>
    public sealed partial class StoreMan : CliMultiCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "store";

        /// <inheritdoc/>
        public StoreMan(ICommandHandler handler)
            : base(handler)
        {}
        #endregion

        /// <inheritdoc/>
        public override IEnumerable<string> SubCommandNames => new[] {Add.Name, Audit.Name, Copy.Name, Export.Name, Find.Name, List.Name, ListImplementations.Name, Manage.Name, Optimise.Name, Purge.Name, Remove.Name, Verify.Name, AddDir.Name, RemoveDir.Name};

        /// <inheritdoc/>
        public override CliCommand GetCommand(string commandName)
            => (commandName ?? throw new ArgumentNullException(nameof(commandName))) switch
            {
                Add.Name => new Add(Handler),
                Audit.Name => new Audit(Handler),
                Copy.Name => new Copy(Handler),
                Export.Name => new Export(Handler),
                Find.Name => new Find(Handler),
                List.Name => new List(Handler),
                ListImplementations.Name => new ListImplementations(Handler),
                Manage.Name => new Manage(Handler),
                "manifest" => throw new NotSupportedException(string.Format(Resources.UseInstead, "0install digest --manifest")),
                Optimise.Name or Optimise.AltName => new Optimise(Handler),
                Purge.Name => new Purge(Handler),
                Remove.Name => new Remove(Handler),
                Verify.Name => new Verify(Handler),
                AddDir.Name => new AddDir(Handler),
                RemoveDir.Name => new RemoveDir(Handler),
                _ => throw new OptionException(string.Format(Resources.UnknownCommand, commandName), commandName)
            };

        public abstract class StoreSubCommand : CliCommand, ICliSubCommand
        {
            public string ParentName => Name;

            protected StoreSubCommand(ICommandHandler handler)
                : base(handler)
            {}

            /// <summary>
            /// Returns the default <see cref="IImplementationStore"/> or a <see cref="CompositeImplementationStore"/> as specified by the <see cref="CliCommand.AdditionalArgs"/>.
            /// </summary>
            protected IImplementationStore GetEffectiveStore()
            {
                if (AdditionalArgs.Count == 0) return ImplementationStore;
                else
                {
                    foreach (string path in AdditionalArgs)
                    {
                        if (!Directory.Exists(path))
                            throw new DirectoryNotFoundException(string.Format(Resources.FileOrDirNotFound, path));
                    }

                    return new CompositeImplementationStore(
                        AdditionalArgs.Select(x => (IImplementationStore)new ImplementationStore(x, useWriteProtection: false)));
                }
            }
        }
    }
}
