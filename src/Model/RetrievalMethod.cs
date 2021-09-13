// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Model
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
        /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
        public virtual void Normalize(FeedUri? feedUri = null) {}

        /// <summary>
        /// Creates a deep copy of this <see cref="RetrievalMethod"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="RetrievalMethod"/>.</returns>
        public abstract RetrievalMethod Clone();
    }
}
