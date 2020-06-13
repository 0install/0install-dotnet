// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace ZeroInstall.Model
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
        public static string ToJsonString(this object? data) => JsonConvert.SerializeObject(data);

        /// <summary>
        /// Loads an object from an JSON string.
        /// </summary>
        /// <typeparam name="T">The type of object the JSON string shall be converted into.</typeparam>
        /// <param name="data">The JSON string to be parsed.</param>
        /// <returns>The deserialized object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to determine the type of returned object")]
        public static T FromJsonString<T>(string data)
            => JsonConvert.DeserializeObject<T>(data ?? throw new ArgumentNullException(nameof(data)));

        /// <summary>
        /// Loads an object from an JSON string using an anonymous type as the target.
        /// </summary>
        /// <typeparam name="T">The type of object the JSON string shall be converted into.</typeparam>
        /// <param name="data">The JSON string to be parsed.</param>
        /// <param name="anonymousType">An instance of the anonymous type to parse to.</param>
        /// <returns>The deserialized object.</returns>
        public static T FromJsonString<T>(string data, T anonymousType)
            => JsonConvert.DeserializeAnonymousType(
                data ?? throw new ArgumentNullException(nameof(data)),
                anonymousType ?? throw new ArgumentNullException(nameof(anonymousType)));

        /// <summary>
        /// Reparses an object previously deserialized from JSON into a different representation.
        /// </summary>
        /// <typeparam name="T">The type of object the data shall be converted into.</typeparam>
        /// <param name="data">The object to be parsed again.</param>
        /// <returns>The deserialized object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to determine the type of returned object")]
        public static T ReparseAsJson<T>(this object data) => FromJsonString<T>(data.ToJsonString());

        /// <summary>
        /// Reparses an object previously deserialized from JSON into a different representation using an anonymous type as the target.
        /// </summary>
        /// <typeparam name="T">The type of object the data shall be converted into.</typeparam>
        /// <param name="data">The object to be parsed again.</param>
        /// <param name="anonymousType">An instance of the anonymous type to parse to.</param>
        /// <returns>The deserialized object.</returns>
        public static T ReparseAsJson<T>(this object data, T anonymousType) => FromJsonString(data.ToJsonString(), anonymousType);
    }
}
