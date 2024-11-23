// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Indicates the <see cref="IOpenPgp"/> implementation detected a problem with a digital signature.
/// </summary>
#if !NET
[Serializable]
#endif
public sealed class SignatureException : Exception
{
    /// <inheritdoc/>
    public SignatureException()
        : base(Resources.InvalidSignature)
    {}

    /// <inheritdoc/>
    public SignatureException(string message)
        : base(message)
    {}

    /// <inheritdoc/>
    public SignatureException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
#if !NET
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private SignatureException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
#endif
    #endregion
}
