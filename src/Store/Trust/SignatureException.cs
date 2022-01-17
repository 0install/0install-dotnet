// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Runtime.Serialization;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Indicates the <see cref="IOpenPgp"/> implementation detected a problem with a digital signature.
/// </summary>
[Serializable]
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
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private SignatureException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
    #endregion
}
