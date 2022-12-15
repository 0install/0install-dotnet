// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// An implementation is a specific version of an application that can be downloaded and executed (e.g. Firefox 3.6 for Windows).
/// </summary>
/// <seealso cref="Feed.Elements"/>
[Description("An implementation is a specific version of an application that can be downloaded and executed (e.g. Firefox 3.6 for Windows).")]
[Serializable, XmlRoot("implementation", Namespace = Feed.XmlNamespace), XmlType("implementation", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Implementation : ImplementationBase
{
    /// <summary>
    /// A list of <see cref="RetrievalMethod"/>s for downloading the implementation.
    /// </summary>
    [Browsable(false)]
    [XmlElement(typeof(Archive)), XmlElement(typeof(SingleFile)), XmlElement(typeof(Recipe))]
    [OrderedEquality]
    public List<RetrievalMethod> RetrievalMethods { get; } = new();

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize(FeedUri? feedUri = null)
    {
        base.Normalize(feedUri);

        // Apply if-0install-version filter
        RetrievalMethods.RemoveAll(FilterMismatch);

        var toRemove = new List<RetrievalMethod>();
        foreach (var retrievalMethod in RetrievalMethods)
        {
            try
            {
                retrievalMethod.Normalize(feedUri);
            }
            #region Error handling
            catch (UriFormatException ex)
            {
                Log.Error(ex.Message, ex);
                toRemove.Add(retrievalMethod);
            }
            #endregion
        }
        RetrievalMethods.Remove(toRemove);
    }
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Implementation"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Implementation"/>.</returns>
    public Implementation CloneImplementation()
    {
        var implementation = new Implementation {ID = ID, Version = Version};
        CloneFromTo(this, implementation);
        implementation.RetrievalMethods.Add(RetrievalMethods.CloneElements());
        return implementation;
    }

    /// <summary>
    /// Creates a deep copy of this <see cref="Implementation"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Implementation"/>.</returns>
    public override Element Clone() => CloneImplementation();
    #endregion
}
