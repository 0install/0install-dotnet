// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Serialization;
using System.Text;
using NanoByte.Common.Dispatch;
using ZeroInstall.Store.Manifests;

#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Indicates that the <see cref="ManifestDigest"/> of an implementation does not match the expected value.
/// </summary>
[Serializable]
public sealed class DigestMismatchException : Exception
{
    /// <summary>
    /// The hash value the <see cref="Implementation"/> was supposed to have.
    /// </summary>
    public string? ExpectedDigest { get; }

    /// <summary>
    /// The <see cref="Manifest"/> that resulted in the <see cref="ExpectedDigest"/>.
    /// </summary>
    public Manifest? ExpectedManifest { get; }

    /// <summary>
    /// The hash value that was actually calculated.
    /// </summary>
    public string? ActualDigest { get; }

    /// <summary>
    /// The <see cref="Manifest"/> that resulted in the <see cref="ActualDigest"/>.
    /// </summary>
    public Manifest? ActualManifest { get; }

    /// <summary>
    /// A longer version of <see cref="Exception.Message"/> that contains more details. Suitable for verbose output.
    /// </summary>
    public string LongMessage
    {
        get
        {
            var builder = new StringBuilder(Message);
            if (ExpectedManifest != null && ActualManifest != null)
            {
                Merge.TwoWay(ActualManifest.Lines, ExpectedManifest.Lines,
                    added: node => builder.Append(Environment.NewLine + "unexpected: " + node),
                    removed: node => builder.Append(Environment.NewLine + "missing: " + node));
            }
            else
            {
                if (ExpectedManifest != null) builder.Append(Environment.NewLine + string.Format(Resources.DigestMismatchExpectedManifest, ExpectedManifest));
                if (ActualManifest != null) builder.Append(Environment.NewLine + string.Format(Resources.DigestMismatchActualManifest, ActualManifest));
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// Creates a new digest mismatch exception.
    /// </summary>
    /// <param name="expectedDigest">The digest value the <see cref="Implementation"/> was supposed to have.</param>
    /// <param name="actualDigest">The digest value that was actually calculated.</param>
    /// <param name="expectedManifest">The <see cref="Manifest"/> that resulted in the <paramref name="expectedDigest"/>; may be <c>null</c>.</param>
    /// <param name="actualManifest">The <see cref="Manifest"/> that resulted in the <paramref name="actualDigest"/>.</param>
    public DigestMismatchException(string? expectedDigest = null, string? actualDigest = null, Manifest? expectedManifest = null, Manifest? actualManifest = null)
        : base(BuildMessage(expectedDigest, actualDigest))
    {
        ExpectedDigest = expectedDigest;
        ActualDigest = actualDigest;
        ExpectedManifest = expectedManifest;
        ActualManifest = actualManifest;
    }

    private static string BuildMessage(string? expectedDigest, string? actualDigest)
    {
        string message = Resources.DigestMismatch;
        if (!string.IsNullOrEmpty(expectedDigest)) message += Environment.NewLine + string.Format(Resources.DigestMismatchExpectedDigest, expectedDigest);
        if (!string.IsNullOrEmpty(actualDigest)) message += Environment.NewLine + string.Format(Resources.DigestMismatchActualDigest, actualDigest);
        return message;
    }

    /// <inheritdoc/>
    public DigestMismatchException()
        : base(BuildMessage(null, null))
    {}

    /// <inheritdoc/>
    public DigestMismatchException(string message)
        : base(message)
    {}

    /// <inheritdoc/>
    public DigestMismatchException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private DigestMismatchException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        #region Sanity checks
        if (info == null) throw new ArgumentNullException(nameof(info));
        #endregion

        ExpectedDigest = info.GetString("ExpectedDigest");
        ExpectedManifest = (Manifest?)info.GetValue("ExpectedManifest", typeof(Manifest));
        ActualDigest = info.GetString("ActualDigest");
        ActualManifest = (Manifest?)info.GetValue("ActualManifest", typeof(Manifest));
    }

    /// <inheritdoc/>
#if NETFRAMEWORK
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        #region Sanity checks
        if (info == null) throw new ArgumentNullException(nameof(info));
        #endregion

        info.AddValue("ExpectedDigest", ExpectedDigest);
        info.AddValue("ExpectedManifest", ExpectedManifest);
        info.AddValue("ActualDigest", ActualDigest);
        info.AddValue("ActualManifest", ActualManifest);

        base.GetObjectData(info, context);
    }
    #endregion
}
