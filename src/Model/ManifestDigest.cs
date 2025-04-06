// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml;

#if !MINIMAL
using ZeroInstall.Model.Design;
#endif

namespace ZeroInstall.Model;

/// <summary>
/// A manifest digest is a means of uniquely identifying an <see cref="Implementation"/> and verifying its contents.
/// </summary>
/// <param name="Sha1">A SHA-1 hash of the old manifest format. Not supported anymore!</param>
/// <param name="Sha1New">A SHA-1 hash of the new manifest format.</param>
/// <param name="Sha256">A SHA-256 hash of the new manifest format. (most secure)</param>
/// <param name="Sha256New">A SHA-256 hash of the new manifest format with a base32 encoding and no equals sign in the path.</param>
/// <remarks>Stores digests of the manifest file using various hashing algorithms.</remarks>
[Description("A manifest digest is a means of uniquely identifying an Implementation and verifying its contents.")]
#if !MINIMAL
[TypeConverter(typeof(ManifestDigestConverter))]
#endif
[Serializable, XmlType("manifest-digest", Namespace = Feed.XmlNamespace)]
public record struct ManifestDigest(
    [property: XmlAttribute("sha1"), DefaultValue(""), Description("A SHA-1 hash of the old manifest format. Not supported anymore!")]
    string? Sha1 = null,
    [property: XmlAttribute("sha1new"), DefaultValue(""), Description("A SHA-1 hash of the new manifest format.")]
    string? Sha1New = null,
    [property: XmlAttribute("sha256"), DefaultValue(""), Description("A SHA-256 hash of the new manifest format.")]
    string? Sha256 = null,
    [property: XmlAttribute("sha256new"), DefaultValue(""), Description("A SHA-256 hash of the new manifest format with a base32 encoding and no equals sign in the path.")]
    string? Sha256New = null)
{
    /// <summary>
    /// Contains any unknown hash algorithms specified as pure XML attributes.
    /// </summary>
    [XmlAnyAttribute, NonSerialized]
    public XmlAttribute[]? UnknownAlgorithms = null;

    /// <summary>
    /// Creates a new manifest digest structure by parsing a string.
    /// </summary>
    /// <param name="value">One or more comma separated digest values.</param>
    /// <exception cref="NotSupportedException"><paramref name="value"/> contains no known digest algorithms.</exception>
    public ManifestDigest(string value)
        : this(null, null)
    {
        #region Sanity checks
        if (value == null) throw new ArgumentNullException(nameof(value));
        #endregion

        foreach (string digest in value.Split(','))
            TryParse(digest);

        if (!AvailableDigests.Any()) throw new NotSupportedException(Resources.NoKnownDigestMethod);
    }

    /// <summary>
    /// Tries to parse a string containing a digest value.
    /// Does nothing if the corresponding algorithm is already set or if the string contains no known digest algorithm.
    /// </summary>
    public void TryParse(string digest)
    {
        #region Sanity checks
        if (digest == null) throw new ArgumentNullException(nameof(digest));
        #endregion

        string? GetIfPrefixed(string prefix)
            => digest.StartsWith(prefix, out string? value) ? value : null;

        // Check for known prefixes (and don't overwrite existing values)
        Sha1 ??= GetIfPrefixed("sha1=");
        Sha1New ??= GetIfPrefixed("sha1new=");
        Sha256 ??= GetIfPrefixed("sha256=");
        Sha256New ??= GetIfPrefixed("sha256new_");
    }

    /// <summary>
    /// The manifest digest of an empty directory.
    /// </summary>
    public static readonly ManifestDigest Empty = new(Sha1New: "da39a3ee5e6b4b0d3255bfef95601890afd80709", Sha256: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", Sha256New: "4OYMIQUY7QOBJGX36TEJS35ZEQT24QPEMSNZGTFESWMRW6CSXBKQ");

    /// <summary>
    /// Lists all contained manifest digests sorted from best (safest) to worst.
    /// </summary>
    [XmlIgnore, Browsable(false)]
    public IEnumerable<string> AvailableDigests
    {
        get
        {
            var result = new List<string>(4);
            if (Sha256New != null) result.Add($"sha256new_{Sha256New}");
            if (Sha256 != null) result.Add($"sha256={Sha256}");
            if (Sha1New != null) result.Add($"sha1new={Sha1New}");
            if (Sha1 != null) result.Add($"sha1={Sha1}");
            return result;
        }
    }

    /// <summary>
    /// Returns the best entry of <see cref="AvailableDigests"/>; <c>null</c> if there are none.
    /// </summary>
    [Browsable(false)]
    public string? Best => AvailableDigests.FirstOrDefault();

    /// <summary>
    /// Returns the manifest digests in the form "sha1new=abc123,sha256new_ABC123,...". Safe for parsing!
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(Sha1)) parts.Add($"sha1={Sha1}");
        if (!string.IsNullOrEmpty(Sha1New)) parts.Add($"sha1new={Sha1New}");
        if (!string.IsNullOrEmpty(Sha256)) parts.Add($"sha256={Sha256}");
        if (!string.IsNullOrEmpty(Sha256New)) parts.Add($"sha256new_{Sha256New}");
        return string.Join(",", parts);
    }

    /// <summary>
    /// Indicates whether this digest is at least partially equal to another one.
    /// </summary>
    /// <remarks>Two digests are considered partially equal if at least one digest algorithm matches and no values are contradictory.</remarks>
    public bool PartialEquals(ManifestDigest other)
    {
        int matchCounter = 0;

        bool Helper(string? left, string? right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right)) return true;
            if (left == right)
            {
                matchCounter++;
                return true;
            }
            else return false;
        }

        return Helper(Sha1, other.Sha1)
            && Helper(Sha1New, other.Sha1New)
            && Helper(Sha256, other.Sha256)
            && Helper(Sha256New, other.Sha256New)
            && (matchCounter > 0);
    }
}
