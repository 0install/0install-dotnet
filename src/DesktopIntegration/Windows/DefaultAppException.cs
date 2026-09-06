// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Indicates that the OS denied changing the Default App for a file type or URL protocol programmatically.
/// </summary>
#if !NET
[Serializable]
#endif
public sealed class DefaultAppException : IOException
{
    /// <inheritdoc/>
    public DefaultAppException()
        : base("Failed to set the Default App.")
    {}

    /// <inheritdoc/>
    public DefaultAppException(string message)
        : base(message)
    {}

    /// <inheritdoc/>
    public DefaultAppException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
#if !NET
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private DefaultAppException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
#endif
    #endregion
}
