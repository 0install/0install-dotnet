// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A file that can be executed directly by the operating system without an additional runtime environment.
    /// </summary>
    public abstract class NativeExecutable : Candidate
    {
        /// <inheritdoc/>
        public override Command CreateCommand() => new Command
        {
            Name = CommandName,
            Path = RelativePath
        };
    }
}
