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
    public sealed record TrustNode(string Fingerprint, Domain Domain) : INamed<TrustNode>
    {
        /// <summary>
        /// The UI path name of this node. Uses a backslash as the separator in hierarchical names.
        /// </summary>
        [Browsable(false)]
        public string Name { get => Fingerprint + "\\" + Domain.Value; set => throw new NotSupportedException(); }

        /// <inheritdoc/>
        int IComparable<TrustNode>.CompareTo(TrustNode? other)
        {
            int fingerprintCompare = string.CompareOrdinal(Fingerprint, other?.Fingerprint);
            return (fingerprintCompare == 0) ? string.CompareOrdinal(Domain.Value, other?.Domain.Value) : fingerprintCompare;
        }
    }
}
