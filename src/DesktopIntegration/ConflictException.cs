// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Serialization;
using ZeroInstall.DesktopIntegration.AccessPoints;

#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Indicates a desktop integration operation could not be completed due to conflicting <see cref="AccessPoint"/>s.
/// </summary>
[Serializable]
public sealed class ConflictException : InvalidOperationException
{
    /// <summary>
    /// The entries that are in conflict with each other.
    /// </summary>
    public IEnumerable<ConflictData>? Entries { get; private set; }

    /// <summary>
    /// Creates an exception indicating a new desktop integration conflict.
    /// </summary>
    /// <param name="existingEntry">The existing entry that is preventing <paramref name="newEntry"/> from being applied.</param>
    /// <param name="newEntry">The new entry that is in conflict with <paramref name="existingEntry"/>.</param>
    public static ConflictException NewConflict(ConflictData existingEntry, ConflictData newEntry)
        => new(string.Format(Resources.AccessPointNewConflict, existingEntry, newEntry))
        {
            Entries = [existingEntry, newEntry]
        };

    /// <summary>
    /// Creates an exception indicating an inner desktop integration conflict.
    /// </summary>
    /// <param name="entries">The entries that are in conflict with each other.</param>
    public static ConflictException InnerConflict(params ConflictData[] entries)
        => new(
            string.Format(Resources.AccessPointInnerConflict, entries[0].AppEntry) + Environment.NewLine +
            string.Join(Environment.NewLine, entries.Select(x => x.AccessPoint.ToString()).WhereNotNull()))
        {
            Entries = entries
        };

    /// <summary>
    /// Creates an exception indicating an existing desktop integration conflict.
    /// </summary>
    /// <param name="entries">The entries that are in conflict with each other.</param>
    public static ConflictException ExistingConflict(params ConflictData[] entries)
        => new(Resources.AccessPointExistingConflict + Environment.NewLine +
               string.Join(Environment.NewLine, entries.Select(x => x.ToString())))
        {
            Entries = entries
        };

    /// <inheritdoc/>
    public ConflictException()
        : base("Unknown conflict")
    {}

    /// <inheritdoc/>
    public ConflictException(string message)
        : base(message)
    {}

    /// <inheritdoc/>
    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private ConflictException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}

    /// <inheritdoc/>
#if NETFRAMEWORK
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);
    #endregion
}
