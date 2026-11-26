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

    /// <summary>
    /// Creates a manifest for a specific browser.
    /// </summary>
    /// <param name="browser">The browser to create the manifest for. Must be one of <see cref="KnownBrowsers"/>.</param>
    /// <param name="name">Name of the native messaging host.</param>
    /// <param name="description">Short application description.</param>
    /// <param name="path">Path to the native messaging host binary.</param>
    /// <param name="extensions">The browser extensions that should have access to the native messaging host.</param>
    public static BrowserNativeMessagingManifest For(string browser, string name, string description, string path, IEnumerable<BrowserExtension> extensions)
    {
        var manifest = new BrowserNativeMessagingManifest(name, description, path);
        if (browser == KnownBrowsers.Firefox)
            manifest.AllowedExtensions = extensions.Select(x => x.ID);
        else
            manifest.AllowedOrigins = extensions.Select(x => $"chrome-extension://{x.ID}/");
        return manifest;
    }
}
