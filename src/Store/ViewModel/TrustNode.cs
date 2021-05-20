// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using NanoByte.Common;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Represents a <see cref="Key"/>-<see cref="Domain"/> pair in a <see cref="TrustDB"/> for display in a UI.
    /// </summary>
    /// <param name="Fingerprint">The <see cref="Key.Fingerprint"/>.</param>
    /// <param name="Domain">The domain the fingerprint is valid for.</param>
    public sealed record TrustNode(string Fingerprint, Domain Domain) : INamed
    {
        /// <summary>
        /// The full name of the node used for tree hierarchies.
        /// </summary>
        [Browsable(false)]
        public string Name
        {
            get => Fingerprint + Named.TreeSeparator + Domain.Value;
            set => throw new NotSupportedException();
        }
    }
}
