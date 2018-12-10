// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// A retrieval method is a way of getting a copy of an <see cref="Implementation"/>.
    /// </summary>
    [XmlType("retrieval-method", Namespace = Feed.XmlNamespace)]
    public abstract class RetrievalMethod : FeedElement, ICloneable<RetrievalMethod>
    {
        /// <summary>
        /// Sets missing default values and handles legacy elements.
        /// </summary>
        /// <param name="feedUri">The feed the data was originally loaded from.</param>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> is relative and <paramref name="feedUri"/> is a remote URI.</exception>
        /// <remarks>This method should be called to prepare a <see cref="Feed"/> for solver processing. Do not call it if you plan on serializing the feed again since it may loose some of its structure.</remarks>
        public virtual void Normalize([CanBeNull] FeedUri feedUri = null) {}

        /// <summary>
        /// Creates a deep copy of this <see cref="RetrievalMethod"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="RetrievalMethod"/>.</returns>
        public abstract RetrievalMethod Clone();
    }
}
