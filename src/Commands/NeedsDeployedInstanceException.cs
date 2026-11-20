// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET
using System.Runtime.Serialization;
#endif

#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace ZeroInstall.Commands;

/// <summary>
/// Indicates that the current Zero Install instance is not deployed to a location suitable for the requested operation.
/// </summary>
[Serializable]
public sealed class NeedsDeployedInstanceException : NotSupportedException
{
    /// <summary>
    /// <c>true</c> if a machine-wide deployment location is required; <c>false</c> if a user-specific location will also do.
    /// </summary>
    public bool NeedsMachineWide { get; }

    /// <inheritdoc/>
    public NeedsDeployedInstanceException() {}

    /// <inheritdoc/>
    public NeedsDeployedInstanceException(string message)
        : base(message)
    {}

    /// <summary>
    /// Creates a new needs deployed instance exception.
    /// </summary>
    /// <param name="message">A message describing why the current location in unsuitable.</param>
    /// <param name="needsMachineWide"><c>true</c> if a machine-wide location is required; <c>false</c> if a user-specific location will also do.</param>
    public NeedsDeployedInstanceException(string message, bool needsMachineWide)
        : base(message)
    {
        NeedsMachineWide = needsMachineWide;
    }

    /// <inheritdoc/>
    public NeedsDeployedInstanceException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
#if !NET
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private NeedsDeployedInstanceException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        #region Sanity checks
        if (info == null) throw new ArgumentNullException(nameof(info));
        #endregion

        NeedsMachineWide = info.GetBoolean("NeedsMachineWide");
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

        info.AddValue("NeedsMachineWide", NeedsMachineWide);

        base.GetObjectData(info, context);
    }
#endif
    #endregion
}
