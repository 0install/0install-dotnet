// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using JetBrains.Annotations;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Services.Native
{
    /// <summary>
    /// Retrieves an implementation by installing it via an external package manager rather than Zero Install itself.
    /// </summary>
    /// <seealso cref="IPackageManager"/>
    public sealed class ExternalRetrievalMethod : RetrievalMethod, IEquatable<ExternalRetrievalMethod>
    {
        /// <summary>
        /// The name of the distribution this package came from.
        /// </summary>
        [CanBeNull]
        public string Distro { get; set; }

        /// <summary>
        /// The package name, in a form recognised by the external package manager.
        /// </summary>
        [CanBeNull]
        public string PackageID { get; set; }

        /// <summary>
        /// The download size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// A question the user shall be asked for confirmation before calling <see cref="Install"/>. <c>null</c> if no confirmation is required.
        /// </summary>
        [CanBeNull]
        public string ConfirmationQuestion { get; set; }

        /// <summary>
        /// A function to call to install this package.
        /// </summary>
        public Action Install { get; set; }

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="ExternalRetrievalMethod"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="ExternalRetrievalMethod"/>.</returns>
        private ExternalRetrievalMethod CloneNativeRetrievalMethod() => new ExternalRetrievalMethod {Distro = Distro, PackageID = PackageID, Size = Size, ConfirmationQuestion = ConfirmationQuestion, Install = Install};

        /// <summary>
        /// Creates a deep copy of this <see cref="ExternalRetrievalMethod"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="ExternalRetrievalMethod"/>.</returns>
        public override RetrievalMethod Clone() => CloneNativeRetrievalMethod();
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ExternalRetrievalMethod other)
        {
            if (other == null) return false;
            return base.Equals(other) && Distro == other.Distro && PackageID == other.PackageID && Size == other.Size && ConfirmationQuestion == other.ConfirmationQuestion && Equals(Install, other.Install);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ExternalRetrievalMethod method && Equals(method);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ Distro?.GetHashCode() ?? 0;
                result = (result * 397) ^ PackageID?.GetHashCode() ?? 0;
                result = (result * 397) ^ Size.GetHashCode();
                result = (result * 397) ^ ConfirmationQuestion?.GetHashCode() ?? 0;
                return result;
            }
        }
        #endregion
    }
}
