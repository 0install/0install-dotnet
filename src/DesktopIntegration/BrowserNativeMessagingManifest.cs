// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NET
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
#endif

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// JSON-serializable browser native messaging host manifest.
/// </summary>
/// <param name="Name">Name of the native messaging host.</param>
/// <param name="Description">Short application description.</param>
/// <param name="Path">Path to the native messaging host binary.</param>
/// <remarks>
/// See
/// https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging
/// and
/// https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/Native_messaging
/// </remarks>
internal record BrowserNativeMessagingManifest(
#if NET
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("path")] string Path
#else
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("description")] string Description,
    [property: JsonProperty("path")] string Path
#endif
)
{
#if NET
    [JsonPropertyName("type")]
#else
    [JsonProperty("type")]
#endif
    public string Type => "stdio";

    /// <summary>
    /// List of extension URLs that should have access to the native messaging host for Chromium-based browsers.
    /// </summary>
#if NET
    [JsonPropertyName("allowed_origins"), JsonIgnoreAttribute(Condition = JsonIgnoreCondition.WhenWritingNull)]
#else
    [JsonProperty("allowed_origins", NullValueHandling = NullValueHandling.Ignore)]
#endif
    public IEnumerable<string>? AllowedOrigins { get; set; }

    /// <summary>
    /// List of extension IDs that should have access to the native messaging host for Mozilla-based browsers.
    /// </summary>
#if NET
    [JsonPropertyName("allowed_extensions"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#else
    [JsonProperty("allowed_extensions", NullValueHandling = NullValueHandling.Ignore)]
#endif
    public IEnumerable<string>? AllowedExtensions { get; set; }
}
