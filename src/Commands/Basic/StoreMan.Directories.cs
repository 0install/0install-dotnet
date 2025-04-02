// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Basic;

partial class StoreMan
{
    private abstract class DirCommand : StoreSubCommand
    {
        public override string Usage => "PATH";
        protected override int AdditionalArgsMin => 1;
        protected override int AdditionalArgsMax => 1;

        /// <summary>Apply the operation machine-wide instead of just for the current user.</summary>
        protected bool MachineWide { get; private set; }

        protected DirCommand(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("m|machine", () => Resources.OptionMachine, _ => MachineWide = true);
        }

        protected string GetPath() => Locations.IsPortable ? AdditionalArgs[0] : Path.GetFullPath(AdditionalArgs[0]);

        protected IEnumerable<string> GetImplementationDirs()
            => MachineWide
                ? ImplementationStores.GetMachineWideDirectories()
                : ImplementationStores.GetUserDirectories();

        protected void SetImplementationDirs(IEnumerable<string> paths)
        {
            if (MachineWide) ImplementationStores.SetMachineWideDirectories(paths);
            else ImplementationStores.SetUserDirectories(paths);
        }
    }

    private class AddDir(ICommandHandler handler) : DirCommand(handler)
    {
        public const string Name = "add-dir";
        public override string Description => Resources.DescriptionStoreAddDir;

        public override ExitCode Execute()
        {
            string path = GetPath();

            // Init new store to ensure the target is suitable
            ImplementationStore = new ImplementationStore(path, Handler);

            var dirs = GetImplementationDirs().ToList();
            if (dirs.AddIfNew(path))
            {
                SetImplementationDirs(dirs);
                return ExitCode.OK;
            }
            else
            {
                Log.Warn(string.Format(Resources.AlreadyInImplDirs, path));
                return ExitCode.NoChanges;
            }
        }
    }

    private class RemoveDir(ICommandHandler handler) : DirCommand(handler)
    {
        public const string Name = "remove-dir";
        public override string Description => Resources.DescriptionStoreRemoveDir;

        public override ExitCode Execute()
        {
            string path = GetPath();

            var dirs = GetImplementationDirs().ToList();
            if (dirs.Remove(path))
            {
                SetImplementationDirs(dirs);
                return ExitCode.OK;
            }
            else
            {
                Log.Warn(string.Format(Resources.NotInImplDirs, path));
                return ExitCode.NoChanges;
            }
        }
    }

    public class List(ICommandHandler handler) : StoreSubCommand(handler)
    {
        public const string Name = "list";
        public override string Description => Resources.DescriptionStoreList;
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        public override ExitCode Execute()
        {
            if (Handler.IsGui) ShowConfig(ConfigTab.Storage);
            else
            {
                var composite = ImplementationStore as CompositeImplementationStore;
                Handler.Output("Stores", (composite == null) ? [ImplementationStore] : composite.Stores);
            }
            return ExitCode.OK;
        }
    }
}
