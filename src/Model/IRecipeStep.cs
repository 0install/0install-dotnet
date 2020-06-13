// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common;

namespace ZeroInstall.Model
{
    /// <summary>
    /// A retrieval step is a part of a <see cref="Recipe"/>.
    /// </summary>
    public interface IRecipeStep : ICloneable<IRecipeStep>
    {
        /// <summary>
        /// Sets missing default values and handles legacy elements.
        /// </summary>
        /// <param name="feedUri">The feed the data was originally loaded from.</param>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> is relative and <paramref name="feedUri"/> is a remote URI.</exception>
        /// <remarks>This method should be called to prepare a <see cref="Feed"/> for solver processing. Do not call it if you plan on serializing the feed again since it may loose some of its structure.</remarks>
        void Normalize(FeedUri? feedUri = null);
    }
}
