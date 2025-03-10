// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Native;
using NanoByte.Common.Values.Design;
using ZeroInstall.Model.Selection;

#if !NET
using System.Runtime.Serialization;
#endif

#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace ZeroInstall.Model;

/// <summary>
/// Represents a feed or interface URI or local path. Unlike <see cref="System.Uri"/> this class only accepts HTTP(S) URLs and absolute local paths.
/// </summary>
[TypeConverter(typeof(StringConstructorConverter<FeedUri>))]
#if !NET
[Serializable]
#endif
[Equatable]
public sealed partial class FeedUri : Uri
#if !NET
    , ISerializable
#endif
{
    #region Prefixes
    /// <summary>
    /// This is prepended to a <see cref="FeedUri"/> if it is meant for demo data and should not be used to actually fetch a feed.
    /// </summary>
    /// <seealso cref="IsFake"/>
    public const string FakePrefix = "fake:";

    /// <summary>
    /// Indicates whether this is a fake identifier meant for demo data and should not be used to actually fetch a feed.
    /// </summary>
    /// <seealso cref="FakePrefix"/>
    public bool IsFake { get; }

    /// <summary>
    /// This is prepended to <see cref="ImplementationSelection.FromFeed"/> if data was pulled from a native package manager rather than the feed itself.
    /// </summary>
    /// <seealso cref="PackageImplementation"/>
    /// <seealso cref="IsFromDistribution"/>
    public const string FromDistributionPrefix = "distribution:";

    /// <summary>
    /// Indicates that an <see cref="ImplementationSelection"/> was generated with data from a native package manager rather than the feed itself.
    /// </summary>
    /// <seealso cref="FromDistributionPrefix"/>
    public bool IsFromDistribution { get; }

    private static string TrimPrefix(string value)
    {
        if (value.StartsWith(FakePrefix, out string? trimmed)) return trimmed;
        else if (value.StartsWith(FromDistributionPrefix, out trimmed)) return trimmed;
        else return value;
    }

    private string PrependPrefix(string result)
    {
        if (IsFake) return FakePrefix + result;
        else if (IsFromDistribution) return FromDistributionPrefix + result;
        else return result;
    }
    #endregion

    /// <summary>
    /// Creates a feed URI from an existing <see cref="Uri"/>.
    /// </summary>
    /// <param name="value">An existing <see cref="Uri"/>.</param>
    /// <exception cref="UriFormatException"><paramref name="value"/> is not a valid HTTP(S) URL or an absolute local path.</exception>
    [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
    public FeedUri(Uri value)
        : this(value.OriginalString)
    {}

    /// <summary>
    /// Passing a <see cref="FeedUri"/> instance into the <see cref="FeedUri"/> constructor does nothing useful. Just use the original object.
    /// </summary>
    [Obsolete("Passing a FeedUri instance into the FeedUri constructor does nothing useful. Just use the original object.")]
    public FeedUri(FeedUri value)
        : this((Uri)value)
    {}

    /// <summary>
    /// Creates a feed URI from a string.
    /// </summary>
    /// <param name="value">A string to parse as an HTTP(S) URL or an absolute local path.</param>
    /// <exception cref="UriFormatException"><paramref name="value"/> is not a valid HTTP(S) URL or an absolute local path.</exception>
    [SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads")]
    public FeedUri([Localizable(false)] string value)
        : base(TrimPrefix(value), UriKind.Absolute)
    {
        if (string.IsNullOrEmpty(value)) throw new UriFormatException();

        if (Scheme is not ("http" or "https" or "file"))
            throw new UriFormatException(string.Format(Resources.InvalidFeedUri, this));

        IsFake = value.StartsWith(FakePrefix);
        IsFromDistribution = value.StartsWith(FromDistributionPrefix);
    }

    #region Escaping
    /// <summary>
    /// Escapes an identifier using URL encoding.
    /// </summary>
    [Pure]
    public static string Escape(string value)
    {
        var builder = new StringBuilder();
        foreach (char t in value ?? throw new ArgumentNullException(nameof(value)))
        {
            builder.Append(t switch
            {
                '-' => '-',
                '_' => '_',
                '.' => '.',
                _ when char.IsLetterOrDigit(t) => t,
                _ => $"%{(int)t:x}"
            });
        }
        return builder.ToString();
    }

    /// <summary>
    /// Escapes the identifier using URL encoding.
    /// </summary>
    public new string Escape() => Escape(AbsoluteUri);

    /// <summary>
    /// Unescapes an identifier using URL encoding.
    /// </summary>
    /// <exception cref="UriFormatException">The unescaped string is not a valid HTTP(S) URL or an absolute local path.</exception>
    public new static FeedUri Unescape(string escaped)
    {
        #region Sanity checks
        if (escaped == null) throw new ArgumentNullException(nameof(escaped));
        #endregion

        var builder = new StringBuilder();
        for (int i = 0; i < escaped.Length; i++)
        {
            switch (escaped[i])
            {
                case '%':
                    if (escaped.Length > i + 2)
                    {
                        builder.Append((char)int.Parse(escaped.Substring(i + 1, 2), NumberStyles.HexNumber));
                        i += 2;
                    }
                    break;

                default:
                    builder.Append(escaped[i]);
                    break;
            }
        }
        return new(builder.ToString());
    }

    /// <summary>
    /// Escapes an identifier using URL encoding except for slashes (encoded as #) and colons (left as-is on POSIX systems).
    /// </summary>
    [Pure]
    public static string PrettyEscape(string value)
    {
        #region Sanity checks
        if (value == null) throw new ArgumentNullException(nameof(value));
        #endregion

        var builder = new StringBuilder();
        foreach (char t in value)
        {
            builder.Append(t switch
            {
                '/' => '#',
                ':' when !UnixUtils.IsUnix => "%3a",
                '-' or '_' or '.' or ':' => t,
                {} when char.IsLetterOrDigit(t) => t,
                _ => $"%{(int)t:x}"
            });
        }
        return builder.ToString();
    }

    /// <summary>
    /// Escapes the identifier using URL encoding except for slashes (encoded as #) and colons (left as-is on POSIX systems).
    /// </summary>
    public string PrettyEscape() => PrettyEscape(AbsoluteUri);

    /// <summary>
    /// Unescapes an identifier using URL encoding except for slashes (encoded as #).
    /// </summary>
    [Pure]
    public static FeedUri PrettyUnescape(string escaped)
    {
        #region Sanity checks
        if (escaped == null) throw new ArgumentNullException(nameof(escaped));
        #endregion

        var builder = new StringBuilder();
        for (int i = 0; i < escaped.Length; i++)
        {
            switch (escaped[i])
            {
                case '#':
                    builder.Append('/');
                    break;

                case '%':
                    if (escaped.Length > i + 2)
                    {
                        builder.Append((char)int.Parse(escaped.Substring(i + 1, 2), NumberStyles.HexNumber));
                        i += 2;
                    }
                    break;

                default:
                    builder.Append(escaped[i]);
                    break;
            }
        }
        return new(builder.ToString());
    }

    /// <summary>
    /// Escape troublesome characters. The result is a valid file leaf name (i.e. does not contain / etc).
    /// Letters, digits, '-', '.', and characters > 127 are copied unmodified.
    /// '/' becomes '__'. Other characters become '_code_', where code is the
    /// lowercase hex value of the character in Unicode.
    /// </summary>
    private static string UnderscoreEscape(string value)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            builder.Append(value[i] switch
            {
                '/' or '\\' => "__",
                '-' => "-",
                '.' when i != 0 => ".", // Avoid creating hidden files, or specials (. and ..)
                {} when value[i] < 128 && !char.IsLetterOrDigit(value[i]) => $"_{(int)value[i]:x}_",
                _ => value[i]
            });
        }
        return builder.ToString();
    }

    /// <summary>
    /// Convert the identifier to a list of path components.
    /// e.g. "http://example.com/foo.xml" becomes ["http", "example.com", "foo.xml"], while
    /// "/root/feed.xml" becomes ["file", "root__feed.xml"].
    /// The number of components is determined by the scheme (three for http, two for file).
    /// Uses [underscore_escape] to escape each component.
    /// </summary>
    public string[] EscapeComponent()
        => Scheme switch
        {
            "http" => ["http", UnderscoreEscape(Host), UnderscoreEscape(LocalPath[1..])],
            "https" => ["https", UnderscoreEscape(Host), UnderscoreEscape(LocalPath[1..])],
            "file" => ["file", UnderscoreEscape(WindowsUtils.IsWindows ? LocalPath : LocalPath[1..])],
            _ => throw new InvalidOperationException()
        };
    #endregion

    #region Serialization
#if !NET
    private FeedUri(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
        IsFake = serializationInfo.GetBoolean("IsFake");
        IsFromDistribution = serializationInfo.GetBoolean("IsFromDistribution");
    }

#if NETFRAMEWORK
    [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
#endif
    void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
        #region Sanity checks
        if (serializationInfo == null) throw new ArgumentNullException(nameof(serializationInfo));
        #endregion

        GetObjectData(serializationInfo, streamingContext);
        serializationInfo.AddValue("IsFake", IsFake);
        serializationInfo.AddValue("IsFromDistribution", IsFromDistribution);
    }
#endif
    #endregion

    #region Conversion
    /// <summary>
    /// Returns a string representation of the URI, not adhering to the escaping rules of RFC 2396.
    /// Not safe for parsing!
    /// </summary>
    public override string ToString() => PrependPrefix(IsFile ? LocalPath : base.ToString());

    /// <summary>
    /// An alternate version of <see cref="ToString"/> that produces results escaped according to RFC 2396.
    /// Safe for parsing!
    /// </summary>
    public string ToStringRfc() => PrependPrefix(IsFile ? LocalPath : AbsoluteUri);
    #endregion
}
