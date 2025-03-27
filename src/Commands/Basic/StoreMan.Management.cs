// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Threading;
using ZeroInstall.Services.Server;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic;

partial class StoreMan
{
    public class ListImplementations(ICommandHandler handler) : StoreSubCommand(handler)
    {
        public const string Name = "list-implementations";
        public const string AltName = "manage";
        public override string Description => Handler.IsGui ? Resources.DescriptionStoreManage : Resources.DescriptionStoreListImplementations;
        public override string Usage => "[FEED-URI]";
        protected override int AdditionalArgsMax => 1;

        public override ExitCode Execute()
        {
            NamedCollection<CacheNode> GetNodes() => new CacheNodeBuilder(Handler, FeedCache, ImplementationStore).Build();

            if (AdditionalArgs is [var feedUri])
            {
                var uri = GetCanonicalUri(feedUri);
                if (uri.IsFile && !File.Exists(uri.LocalPath))
                    throw new FileNotFoundException(string.Format(Resources.FileOrDirNotFound, uri.LocalPath), uri.LocalPath);

                Handler.Output(Resources.CachedImplementations, GetNodes().OfType<ImplementationNode>().Where(x => x.FeedUri == uri));
            }
            else
            {
                if (Handler.IsGui)
                    Handler.Output(Resources.CachedImplementations, GetNodes()); // Tree view
                else
                    Handler.Output(Resources.CachedImplementations, GetNodes().OfType<ImplementationNode>()); // Table view
            }

            return ExitCode.OK;
        }
    }

    public class Audit(ICommandHandler handler) : StoreSubCommand(handler)
    {
        public const string Name = "audit";
        public override string Description => Resources.DescriptionStoreAudit;
        public override string Usage => "[CACHE-DIR+]";

        public override ExitCode Execute()
        {
            SetStorePaths(AdditionalArgs);
            ImplementationStore.Audit(Handler);
            return ExitCode.OK;
        }
    }

    public class Optimise(ICommandHandler handler) : StoreSubCommand(handler)
    {
        public const string Name = "optimise";

        public const string AltName = "optimize";
        public override string Description => Resources.DescriptionStoreOptimise;
        public override string Usage => "[CACHE-DIR+]";

        public override ExitCode Execute()
        {
            SetStorePaths(AdditionalArgs);

            long savedBytes = ImplementationStore.Optimise();
            Handler.OutputLow(Resources.OptimiseComplete, string.Format(Resources.StorageReclaimed, savedBytes.FormatBytes()));
            return ExitCode.OK;
        }
    }

    public class Purge(ICommandHandler handler) : StoreSubCommand(handler)
    {
        public const string Name = "purge";
        public override string Description => Resources.DescriptionStorePurge;
        public override string Usage => "[CACHE-DIR+]";

        public override ExitCode Execute()
        {
            SetStorePaths(AdditionalArgs);

            if (Handler.Ask(Resources.ConfirmPurge, defaultAnswer: true))
            {
                ImplementationStore.Purge();
                return ExitCode.OK;
            }
            else return ExitCode.NoChanges;
        }
    }

    public class Serve(ICommandHandler handler) : StoreSubCommand(handler)
    {
        public const string Name = "serve";
        public override string Description => Resources.DescriptionStoreServe;
        public override string Usage => "[PORT]";
        protected override int AdditionalArgsMax => 1;

        public override ExitCode Execute()
        {
            using var server = new ImplementationServer(ImplementationStore, GetPort());
            Handler.RunTask(new WaitTask(string.Format(Resources.ServingImplementations, server.Port)));

            return ExitCode.OK;
        }

        private ushort GetPort()
        {
            if (AdditionalArgs is []) return 0;

            try
            {
                return ushort.Parse(AdditionalArgs[0]);
            }
            #region Error handling
            catch (OverflowException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new FormatException(ex.Message, ex);
            }
            #endregion
        }
    }
}
