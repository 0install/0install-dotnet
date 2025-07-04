// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Newtonsoft.Json;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// JSON-serializable browser native messaging host manifest, as defined here: https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging
/// </summary>
/// <param name="Name">Name of the native messaging host.</param>
/// <param name="Description">Short application description.</param>
/// <param name="Path">Path to the native messaging host binary.</param>
/// <param name="AllowedOrigins">List of extensions that should have access to the native messaging host.</param>
internal record BrowserNativeMessagingManifest(
    [JsonProperty("name")] string Name,
    [JsonProperty("description")] string Description,
    [JsonProperty("path")] string Path,
    [JsonProperty("allowed_origins")] IEnumerable<string> AllowedOrigins)
{
    [JsonProperty("type")]
    public string Type => "stdio";
}
