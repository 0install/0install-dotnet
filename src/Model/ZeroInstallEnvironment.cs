// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Model;

/// <summary>
/// Environment variables holding command-lines for launching Zero Install.
/// </summary>
public static class ZeroInstallEnvironment
{
    /// <summary>
    /// The command-line version of Zero Install.
    /// </summary>
    public static string? Cli
    {
        get => Environment.GetEnvironmentVariable("ZEROINSTALL");
        set => Environment.SetEnvironmentVariable("ZEROINSTALL", value);
    }

    /// <summary>
    /// The graphical version of Zero Install.
    /// </summary>
    public static string? Gui
    {
        get => Environment.GetEnvironmentVariable("ZEROINSTALL_GUI");
        set => Environment.SetEnvironmentVariable("ZEROINSTALL_GUI", value);
    }

    /// <summary>
    /// Command that downloads a set of <see cref="Implementation"/>s piped in as XML via stdin.
    /// </summary>
    public static string? ExternalFetch
    {
        get => Environment.GetEnvironmentVariable("ZEROINSTALL_EXTERNAL_FETCHER");
        set => Environment.SetEnvironmentVariable("ZEROINSTALL_EXTERNAL_FETCHER", value);
    }

    /// <summary>
    /// The URI of the feed used to start this program.
    /// </summary>
    public static FeedUri? FeedUri
    {
        get
        {
            string? uri = Environment.GetEnvironmentVariable("ZEROINSTALL_FEED_URI");
            try
            {
                if (!string.IsNullOrEmpty(uri)) return new FeedUri(uri);
            }
            catch (UriFormatException)
            {}
            return null;
        }
    }
}
