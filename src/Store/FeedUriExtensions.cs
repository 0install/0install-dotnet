// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using JetBrains.Annotations;
using NanoByte.Common;

namespace ZeroInstall.Store
{
    /// <summary>
    /// Provides extensions methods for <see cref="FeedUri"/>-related types.
    /// </summary>
    public static class FeedUriExtensions
    {
        /// <summary>
        /// Wraps a <see cref="FeedUri"/> pointer in a <see cref="string"/> pointer.
        /// </summary>
        public static PropertyPointer<string> ToStringPointer([NotNull] this PropertyPointer<FeedUri> pointer)
        {
            #region Sanity checks
            if (pointer == null) throw new ArgumentNullException(nameof(pointer));
            #endregion

            return new PropertyPointer<string>(
                getValue: () => pointer.Value?.ToStringRfc(),
                setValue: value => pointer.Value = (value == null) ? null : new FeedUri(value),
                defaultValue: pointer.DefaultValue?.ToStringRfc());
        }
    }
}
