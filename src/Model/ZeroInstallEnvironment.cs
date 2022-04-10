// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;

namespace ZeroInstall.Model;

/// <summary>
/// Provides access to Zero Install-related environment variables.
/// </summary>
public static class ZeroInstallEnvironment
{
    private const string
        CliName = "ZEROINSTALL",
        GuiName = "ZEROINSTALL_GUI",
        ExternalFetcherName = "ZEROINSTALL_EXTERNAL_FETCHER",
        FeedUriName = "ZEROINSTALL_FEED_URI";

    /// <summary>
    /// A command-line for launching the CLI version of Zero Install.
    /// </summary>
    public static string? Cli
    {
        get => Environment.GetEnvironmentVariable(CliName);
        set => Environment.SetEnvironmentVariable(CliName, value);
    }

    /// <summary>
    /// A command-line for launching the graphical version of Zero Install.
    /// </summary>
    public static string? Gui
    {
        get => Environment.GetEnvironmentVariable(GuiName);
        set => Environment.SetEnvironmentVariable(GuiName, value);
    }

    /// <summary>
    /// A command-line that downloads a set of <see cref="Implementation"/>s piped in as XML via stdin.
    /// </summary>
    public static string? ExternalFetch
    {
        get => Environment.GetEnvironmentVariable(ExternalFetcherName);
        set => Environment.SetEnvironmentVariable(ExternalFetcherName, value);
    }

    /// <summary>
    /// The URI of the feed used to start this program.
    /// </summary>
    public static FeedUri? FeedUri
    {
        get
        {
            string? uri = Environment.GetEnvironmentVariable(FeedUriName);
            try
            {
                if (!string.IsNullOrEmpty(uri)) return new FeedUri(uri);
            }
            catch (UriFormatException)
            {}
            return null;
        }
    }

    /// <summary>
    /// Passes a program the feed URI used to start it as an environment variable.
    /// </summary>
    public static void SetFeedUri(this ProcessStartInfo startInfo, FeedUri feedUri)
        => startInfo.EnvironmentVariables[FeedUriName] = feedUri.ToStringRfc();
}
