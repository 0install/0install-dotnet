// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Indicates that the <see cref="IExecutor"/> was unable to launch the desired application.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public sealed class ExecutorException : Exception
{
    /// <summary>
    /// Creates a new missing main exception.
    /// </summary>
    public ExecutorException() {}

    /// <summary>
    /// Creates a new missing main exception.
    /// </summary>
    public ExecutorException(string message)
        : base(message)
    {}

    /// <summary>
    /// Creates a new missing main exception.
    /// </summary>
    public ExecutorException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
#if !NET8_0_OR_GREATER
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private ExecutorException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
#endif
    #endregion
}
