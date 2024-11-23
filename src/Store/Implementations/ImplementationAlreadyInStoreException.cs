// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET
using System.Runtime.Serialization;
#endif

#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Indicates an <see cref="Implementation"/> being added to an <see cref="IImplementationStore"/> is already in the store.
/// </summary>
#if !NET
[Serializable]
#endif
public sealed class ImplementationAlreadyInStoreException : Exception
{
    #region Properties
    /// <summary>
    /// The hash value the <see cref="Implementation"/> was supposed to have.
    /// </summary>
    public ManifestDigest ManifestDigest { get; }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new implementation already in store exception.
    /// </summary>
    /// <param name="manifestDigest">The digest of the <see cref="Implementation"/> that was supposed to be added.</param>
    public ImplementationAlreadyInStoreException(ManifestDigest manifestDigest)
        : base(string.Format(Resources.ImplementationAlreadyInStore, manifestDigest))
    {
        ManifestDigest = manifestDigest;
    }

    /// <inheritdoc/>
    public ImplementationAlreadyInStoreException()
        : base(string.Format(Resources.ImplementationAlreadyInStore, "unknown"))
    {}

    /// <inheritdoc/>
    public ImplementationAlreadyInStoreException(string message)
        : base(message)
    {}

    /// <inheritdoc/>
    public ImplementationAlreadyInStoreException(string message, Exception innerException)
        : base(message, innerException)
    {}
    #endregion

    #region Serialization
#if !NET
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private ImplementationAlreadyInStoreException(SerializationInfo info, StreamingContext context)
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

