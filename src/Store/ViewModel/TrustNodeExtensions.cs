// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using NanoByte.Common.Collections;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Maps between <see cref="TrustDB"/> and <see cref="TrustNode"/>s.
/// </summary>
public static class TrustNodeExtensions
{
    /// <summary>
    /// Creates <see cref="TrustNode"/> representations for all entries in a <see cref="TrustDB"/>.
    /// </summary>
    public static NamedCollection<TrustNode> ToNodes(this TrustDB trustDB)
    {
        #region Sanity checks
        if (trustDB == null) throw new ArgumentNullException(nameof(trustDB));
        #endregion

        var nodes = new NamedCollection<TrustNode>();
        foreach (var key in trustDB.Keys)
        {
            foreach (var domain in key.Domains)
            {
                if (key.Fingerprint != null)
                    nodes.Add(new TrustNode(key.Fingerprint, domain));
            }
        }
        return nodes;
    }

    /// <summary>
    /// Creates a <see cref="TrustDB"/> from <see cref="TrustNode"/>s.
    /// </summary>
    public static TrustDB ToTrustDB(this IEnumerable<TrustNode> nodes)
    {
        #region Sanity checks
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        #endregion

        var trustDB = new TrustDB();
        foreach (var node in nodes)
            trustDB.TrustKey(node.Fingerprint, node.Domain);
        return trustDB;
    }
}
