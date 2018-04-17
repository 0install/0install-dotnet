// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Indicates an <see cref="Implementation"/> could not be found in a <see cref="IStore"/>.
    /// </summary>
    [Serializable]
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

        /// <summary>
        /// Deserializes an exception.
        /// </summary>
        private ImplementationNotFoundException(SerializationInfo info, StreamingContext context)
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
