// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Serialization;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// A set of alphabetically sorted <see cref="Domain"/>s.
/// </summary>
[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "A Set is a specific type of Collection.")]
[Serializable]
public class DomainSet : SortedSet<Domain>
{
    public DomainSet()
        : base(new DomainComparer())
    {}

    public Domain this[int index] => this.Skip(index).First();

    protected DomainSet(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}

    private class DomainComparer : IComparer<Domain>
    {
        public int Compare(Domain x, Domain y) => string.Compare(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);
    }

    #region Conversion
    /// <summary>
    /// Returns the list of domains in the form "Domain1, Domain2, ...". Safe for parsing!
    /// </summary>
    public override string ToString() => string.Join(", ", this.Select(x => x.ToString()!));
    #endregion
}
