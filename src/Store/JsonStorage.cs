// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ZeroInstall.Store
{
    /// <summary>
    /// Provides easy serialization to JSON files.
    /// </summary>
    public static class JsonStorage
    {
        /// <summary>
        /// Returns an object as an JSON string.
        /// </summary>
        /// <param name="data">The object to be stored.</param>
        /// <returns>A string containing the JSON code.</returns>
        [NotNull]
        public static string ToJsonString([CanBeNull] this object data) => JsonConvert.SerializeObject(data);

        /// <summary>
        /// Loads an object from an JSON string.
        /// </summary>
        /// <typeparam name="T">The type of object the JSON string shall be converted into.</typeparam>
        /// <param name="data">The JSON string to be parsed.</param>
        /// <returns>The deserialized object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to determine the type of returned object")]
        public static T FromJsonString<T>([NotNull] string data)
        {
            #region Sanity checks
            if (data == null) throw new ArgumentNullException(nameof(data));
            #endregion

            return JsonConvert.DeserializeObject<T>(data);
        }

        /// <summary>
        /// Loads an object from an JSON string using an anonymous type as the target.
        /// </summary>
        /// <typeparam name="T">The type of object the JSON string shall be converted into.</typeparam>
        /// <param name="data">The JSON string to be parsed.</param>
        /// <param name="anonymousType">An instance of the anonymous type to parse to.</param>
        /// <returns>The deserialized object.</returns>
        public static T FromJsonString<T>([NotNull] string data, [NotNull] T anonymousType)
        {
            #region Sanity checks
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (anonymousType == null) throw new ArgumentNullException(nameof(anonymousType));
            #endregion

            return JsonConvert.DeserializeAnonymousType(data, anonymousType);
        }

        /// <summary>
        /// Reparses an object previously deserialized from JSON into a different representation.
        /// </summary>
        /// <typeparam name="T">The type of object the data shall be converted into.</typeparam>
        /// <param name="data">The object to be reparsed.</param>
        /// <returns>The deserialized object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to determine the type of returned object")]
        [NotNull]
        public static T ReparseAsJson<T>([NotNull] this object data) => FromJsonString<T>(data.ToJsonString());

        /// <summary>
        /// Reparses an object previously deserialized from JSON into a different representation using an anonymous type as the target.
        /// </summary>
        /// <typeparam name="T">The type of object the data shall be converted into.</typeparam>
        /// <param name="data">The object to be reparsed.</param>
        /// <param name="anonymousType">An instance of the anonymous type to parse to.</param>
        /// <returns>The deserialized object.</returns>
        [NotNull]
        public static T ReparseAsJson<T>([NotNull] this object data, [NotNull] T anonymousType) => FromJsonString(data.ToJsonString(), anonymousType);
    }
}
