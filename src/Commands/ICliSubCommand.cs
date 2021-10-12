// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Interface for <see cref="CliCommand"/>s that are aggregated by a <see cref="CliMultiCommand"/>.
    /// </summary>
    public interface ICliSubCommand
    {
        /// <summary>
        /// The name of the <see cref="CliMultiCommand"/> this command is a sub-command of.
        /// </summary>
        [Localizable(false)]
        string ParentName { get; }
    }
}
