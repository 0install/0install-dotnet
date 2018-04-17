// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Indicates an <see cref="Implementation"/> being added to an <see cref="IStore"/> is already in the store.
    /// </summary>
    [Serializable]
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

        /// <summary>
        /// Deserializes an exception.
        /// </summary>
        private ImplementationAlreadyInStoreException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            #region Sanity checks
            if (info == null) throw new ArgumentNullException(nameof(info));
            #endregion

            ManifestDigest = (ManifestDigest)info.GetValue("ManifestDigest", typeof(ManifestDigest));
        }
        #endregion

        #region Serialization
        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            #region Sanity checks
            if (info == null) throw new ArgumentNullException(nameof(info));
            #endregion

            info.AddValue("ManifestDigest", ManifestDigest);

            base.GetObjectData(info, context);
        }
        #endregion
    }
}
