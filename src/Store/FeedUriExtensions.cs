// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common;

namespace ZeroInstall.Store
{
    /// <summary>
    /// Provides extensions methods for <see cref="FeedUri"/>-related types.
    /// </summary>
    public static class FeedUriExtensions
    {
        /// <summary>
        /// Wraps a <see cref="FeedUri"/> pointer in a <see cref="string"/> pointer. Maps empty strings to <c>null</c> URIs.
        /// </summary>
        public static PropertyPointer<string> ToStringPointer(this PropertyPointer<FeedUri?> pointer)
            => PropertyPointer.For(
                getValue: () => pointer.Value?.ToStringRfc() ?? "",
                setValue: value => pointer.Value = string.IsNullOrEmpty(value) ? default : new FeedUri(value),
                defaultValue: pointer.DefaultValue?.ToStringRfc() ?? "");
    }
}
