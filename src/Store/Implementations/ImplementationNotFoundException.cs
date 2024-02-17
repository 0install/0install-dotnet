// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Indicates an <see cref="Implementation"/> could not be found in a <see cref="IImplementationStore"/>.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public sealed class ImplementationNotFoundException : IOException
{
    #region Properties
    /// <summary>
    /// The <see cref="ManifestDigest"/> of the <see cref="Implementation"/> to be found.
    /// </summary>
    public ManifestDigest ManifestDigest { get; }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new implementation not found exception.
    /// </summary>
    /// <param name="manifestDigest">The <see cref="ManifestDigest"/> of the <see cref="Implementation"/> to be found.</param>
    public ImplementationNotFoundException(ManifestDigest manifestDigest)
        : base(string.Format(Resources.ImplementationNotFound, manifestDigest))
    {
        ManifestDigest = manifestDigest;
    }

    /// <summary>
    /// Creates a new implementation not found exception without specifying the specific implementation ID.
    /// </summary>
    public ImplementationNotFoundException()
        : base(string.Format(Resources.ImplementationNotFound, "unknown"))
    {}

    /// <inheritdoc/>
    public ImplementationNotFoundException(string message)
        : base(message)
    {}

    /// <inheritdoc/>
    public ImplementationNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {}
    #endregion

    #region Serialization
#if !NET8_0_OR_GREATER
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private ImplementationNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        #region Sanity checks
        if (info == null) throw new ArgumentNullException(nameof(info));
        #endregion

        ManifestDigest = (ManifestDigest)info.GetValue("ManifestDigest", typeof(ManifestDigest))!;
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

        info.AddValue("ManifestDigest", ManifestDigest);

        base.GetObjectData(info, context);
    }
#endif
    #endregion
}

