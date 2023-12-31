// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Manages the contents of the <see cref="IImplementationStore"/>s.
/// </summary>
public sealed partial class StoreMan(ICommandHandler handler) : CliMultiCommand(handler)
{
    public const string Name = "store";

    /// <inheritdoc/>
    public override IEnumerable<string> SubCommandNames => [Add.Name, Audit.Name, Copy.Name, Export.Name, Find.Name, List.Name, ListImplementations.Name, Optimise.Name, Purge.Name, Remove.Name, Verify.Name, Serve.Name, AddDir.Name, RemoveDir.Name];

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
            ListImplementations.Name or ListImplementations.AltName => new ListImplementations(Handler),
            "manifest" => throw new NotSupportedException(string.Format(Resources.UseInstead, "0install digest --manifest")),
            Optimise.Name or Optimise.AltName => new Optimise(Handler),
            Purge.Name => new Purge(Handler),
            Remove.Name => new Remove(Handler),
            Verify.Name => new Verify(Handler),
            Serve.Name => new Serve(Handler),
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
        /// Sets the paths of the directories to use as <see cref="ImplementationStore"/>s.
        /// Keeps using the defaults if the list is empty.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">A specified directory does not exist.</exception>
        protected void SetStorePaths(IReadOnlyList<string> paths)
        {
            if (paths.Count == 0) return;

            foreach (string path in paths)
            {
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException(string.Format(Resources.FileOrDirNotFound, path));
            }

            ImplementationStore = new CompositeImplementationStore(paths.Select(x => new ImplementationStore(x, Handler, useWriteProtection: false)).ToList());
        }

        /// <summary>
        /// Adds a new implementation to the <see cref="ImplementationStore"/>.
        /// </summary>
        /// <param name="manifestDigest">The digest the implementation is supposed to match.</param>
        /// <param name="build">Callback for building the implementation.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        /// <exception cref="DigestMismatchException">The implementation's content doesn't match the <paramref name="manifestDigest"/>.</exception>
        protected ExitCode AddToStore(ManifestDigest manifestDigest, Action<IBuilder> build)
        {
            try
            {
                ImplementationStore.Add(manifestDigest, build);
                return ExitCode.OK;
            }
            #region Error handling
            catch (ImplementationAlreadyInStoreException ex)
            {
                Log.Warn(ex);
                return ExitCode.NoChanges;
            }
            #endregion
        }
    }
}
