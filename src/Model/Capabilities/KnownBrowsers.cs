// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Well-known web broswers.
/// </summary>
public static class KnownBrowsers
{
    public const string
        Firefox = "Firefox",
        Chrome = "Chrome",
        Chromium = "Chromium",
        Edge = "Edge",
        Opera = "Opera",
        Brave = "Brave",
        Vivaldi = "Vivaldi";

    public static readonly string[] All = [Firefox, Chrome, Chromium, Edge, Opera, Brave, Vivaldi];
}
