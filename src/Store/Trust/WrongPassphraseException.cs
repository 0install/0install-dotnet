// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Serialization;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Indicates that an incorrect passphrase was passed to <see cref="GnuPG"/>.
/// </summary>
[Serializable]
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
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private WrongPassphraseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
    #endregion
}
