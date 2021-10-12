// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic
{
    partial class StoreMan
    {
        public class Manage : StoreSubCommand
        {
            public const string Name = "manage";
            public override string Description => Resources.DescriptionStoreManage;
            public override string Usage => "";
            protected override int AdditionalArgsMax => 0;

            public Manage(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                Handler.ManageStore(ImplementationStore, FeedCache);
                return ExitCode.OK;
            }
        }

        public class ListImplementations : StoreSubCommand
        {
            public const string Name = "list-implementations";
            public override string Description => Resources.DescriptionStoreListImplementations;
            public override string Usage => "[FEED-URI]";
            protected override int AdditionalArgsMax => 1;

            public ListImplementations(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                var nodeBuilder = new CacheNodeBuilder(ImplementationStore, FeedCache);
                nodeBuilder.Run();

                if (AdditionalArgs.Count == 1)
                {
                    var uri = GetCanonicalUri(AdditionalArgs[0]);
                    if (uri.IsFile && !File.Exists(uri.LocalPath))
                        throw new FileNotFoundException(string.Format(Resources.FileOrDirNotFound, uri.LocalPath), uri.LocalPath);

                    var nodes = nodeBuilder.Nodes!.OfType<OwnedImplementationNode>().Where(x => x.FeedUri == uri);
                    Handler.Output(Resources.CachedImplementations, nodes);
                }
                else
                {
                    var nodes = nodeBuilder.Nodes!.OfType<ImplementationNode>();
                    Handler.Output(Resources.CachedImplementations, nodes);
                }

                return ExitCode.OK;
            }
        }

        public class Audit : StoreSubCommand
        {
            public const string Name = "audit";
            public override string Description => Resources.DescriptionStoreAudit;
            public override string Usage => "[CACHE-DIR+]";

            public Audit(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                var store = GetEffectiveStore();
                Handler.RunTask(ForEachTask.Create(
                    name: Resources.StoreAudit,
                    target: store.ListAll().ToList(),
                    work: digest => store.Verify(digest, Handler)));
                return ExitCode.OK;
            }
        }

        public class Optimise : StoreSubCommand
        {
            public const string Name = "optimise";

            public const string AltName = "optimize";
            public override string Description => Resources.DescriptionStoreOptimise;
            public override string Usage => "[CACHE-DIR+]";

            public Optimise(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                long savedBytes = GetEffectiveStore().Optimise(Handler);
                Handler.OutputLow(Resources.OptimiseComplete, string.Format(Resources.StorageReclaimed, savedBytes.FormatBytes()));
                return ExitCode.OK;
            }
        }

        public class Purge : StoreSubCommand
        {
            public const string Name = "purge";
            public override string Description => Resources.DescriptionStorePurge;
            public override string Usage => "[CACHE-DIR+]";

            public Purge(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                if (Handler.Ask(Resources.ConfirmPurge, defaultAnswer: true))
                {
                    GetEffectiveStore().Purge(Handler);
                    return ExitCode.OK;
                }
                else return ExitCode.NoChanges;
            }
        }
    }
}
