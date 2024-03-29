// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// A retrieval step is a part of a <see cref="Recipe"/>.
/// </summary>
public interface IRecipeStep : ICloneable<IRecipeStep>
{
    /// <summary>
    /// Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <param name="feedUri">The feed the data was originally loaded from.</param>
    /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> is relative and <paramref name="feedUri"/> is a remote URI.</exception>
    void Normalize(FeedUri? feedUri = null);
}
