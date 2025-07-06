// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Newtonsoft.Json;

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
    [JsonProperty("name")] string Name,
    [JsonProperty("description")] string Description,
    [JsonProperty("path")] string Path)
{
    [JsonProperty("type")]
    public string Type => "stdio";

    /// <summary>
    /// List of extension URLs that should have access to the native messaging host for Chromium-based browsers.
    /// </summary>
    [JsonProperty("allowed_origins", NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<string>? AllowedOrigins { get; set; }

    /// <summary>
    /// List of extension IDs that should have access to the native messaging host for Mozilla-based browsers.
    /// </summary>
    [JsonProperty("allowed_extensions", NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<string>? AllowedExtensions { get; set; }
}
