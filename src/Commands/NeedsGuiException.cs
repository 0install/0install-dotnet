// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.Commands;

/// <summary>
/// Indicates that the requested operation requires a GUI but the current process does not have one.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class NeedsGuiException : NotSupportedException
{
    /// <inheritdoc/>
    public NeedsGuiException()
        : this(Resources.NeedsGui)
    {}

    /// <inheritdoc/>
    public NeedsGuiException(string message, Exception inner)
        : base(message, inner)
    {}

    /// <inheritdoc/>
    public NeedsGuiException(string message)
        : base(message)
    {}

#if !NET8_0_OR_GREATER
    /// <inheritdoc/>
    protected NeedsGuiException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
#endif
}
