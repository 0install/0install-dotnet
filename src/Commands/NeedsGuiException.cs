// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Serialization;

namespace ZeroInstall.Commands;

/// <summary>
/// Indicates that the requested operation requires a GUI but the current process does not have one.
/// </summary>
[Serializable]
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

    /// <inheritdoc/>
    protected NeedsGuiException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
}
