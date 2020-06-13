// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Models information about an implementation in an <see cref="IImplementationStore"/> without a known owning interface for display in a UI.
    /// </summary>
    public sealed class OrphanedImplementationNode : ImplementationNode
    {
        /// <summary>
        /// Creates a new orphaned implementation node.
        /// </summary>
        /// <param name="digest">The digest identifying the implementation.</param>
        /// <param name="implementationStore">The <see cref="IImplementationStore"/> the implementation is located in.</param>
        /// <exception cref="FormatException">The manifest file is not valid.</exception>
        /// <exception cref="IOException">The manifest file could not be read.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        public OrphanedImplementationNode(ManifestDigest digest, IImplementationStore implementationStore)
            : base(digest, implementationStore)
        {}

        /// <inheritdoc/>
        public override string Name { get => Resources.UnknownInterface + "\\" + Digest + (SuffixCounter == 0 ? "" : " " + SuffixCounter); set => throw new NotSupportedException(); }
    }
}
