// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Indicates that an incorrect passphrase was passed to <see cref="GnuPG"/>.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public sealed class WrongPassphraseException : Exception
{
    /// <summary>
    /// Indicates that an incorrect passphrase was passed to <see cref="GnuPG"/>.
    /// </summary>
    public WrongPassphraseException()
        : base(Resources.WrongPassphrase)
    {}

    /// <inheritdoc/>
    public WrongPassphraseException(string message)
        : base(message)
    {}

    /// <inheritdoc/>
    public WrongPassphraseException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
#if !NET8_0_OR_GREATER
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private WrongPassphraseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
#endif
    #endregion
}
