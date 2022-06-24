// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Client;

/// <summary>
/// Client for invoking Zero Install commands from within other applications.
/// </summary>
public interface IZeroInstallClient
{
    /// <summary>
    /// Trusts feeds from a specific domain when signed with a specific key.
    /// </summary>
    /// <param name="fingerprint">The fingerprint of the key to trust.</param>
    /// <param name="domain">The domain the key should be trusted for.</param>
    void TrustKey(string fingerprint, string domain);

    /// <summary>
    /// Selects a program and compatible versions of all of its dependencies.
    /// </summary>
    /// <param name="requirements">The requirements describing the program.</param>
    /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
    /// <param name="offline">Do not refresh feeds even if they are out-of-date and don't select newer versions of programs for downloading even if they are already known.</param>
    /// <returns>The selected implementations.</returns>
    /// <exception cref="IOException">0install could not be launched or reported a problem accessing the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">0install reported that access to a resource was denied.</exception>
    /// <exception cref="WebException">0install reported a problem downloading a file.</exception>
    /// <exception cref="InvalidDataException">0install reported a problem parsing a file or an invalid signature.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the operation.</exception>
    /// <exception cref="ExitCodeException">0install returned another error.</exception>
    Task<Selections> SelectAsync(Requirements requirements, bool refresh = false, bool offline = false);

    /// <summary>
    /// Downloads a program and compatible versions of all of its dependencies.
    /// </summary>
    /// <param name="requirements">The requirements describing the program.</param>
    /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
    /// <returns>The downloaded implementations.</returns>
    /// <exception cref="IOException">0install could not be launched or reported a problem accessing the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">0install reported that access to a resource was denied.</exception>
    /// <exception cref="WebException">0install reported a problem downloading a file.</exception>
    /// <exception cref="InvalidDataException">0install reported a problem parsing a file, an invalid signature or digest mismatch.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the operation.</exception>
    /// <exception cref="ExitCodeException">0install returned another error.</exception>
    Task<Selections> DownloadAsync(Requirements requirements, bool refresh = false);

    /// <summary>
    /// Runs a program via Zero Install. Does not wait for the program to exit.
    /// </summary>
    /// <param name="requirements">The requirements describing the program.</param>
    /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
    /// <param name="needsTerminal">Indicates that the program requires a terminal in order to run.</param>
    /// <param name="arguments">Additional arguments to pass to the program.</param>
    /// <exception cref="IOException">0install could not be launched or reported a problem accessing the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">0install reported that access to a resource was denied.</exception>
    /// <exception cref="WebException">0install reported a problem downloading a file.</exception>
    /// <exception cref="InvalidDataException">0install reported a problem parsing a file, an invalid signature or digest mismatch.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the operation.</exception>
    /// <exception cref="ExitCodeException">0install or the target program returned an error.</exception>
    void Run(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments);

    /// <summary>
    /// Provides a <see cref="ProcessStartInfo"/> for running a program via Zero Install.
    /// This allows you to wait for the program to exit and/or to capture its output.
    /// </summary>
    /// <param name="requirements">The requirements describing the program.</param>
    /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
    /// <param name="needsTerminal">Indicates that the program requires a terminal in order to run.</param>
    /// <param name="arguments">Additional arguments to pass to the program.</param>
    ProcessStartInfo GetRunStartInfo(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments);

    /// <summary>
    /// Returns the desktop integration categories that are currently applied for a specific feed.
    /// </summary>
    /// <param name="uri">The feed URI of the application.</param>
    /// <param name="machineWide">Get machine-wide desktop integration instead of just for the current user.</param>
    /// <returns>The access point categories (e.g., <c>capability-registration</c>, <c>menu-entry</c>, <c>desktop-icon</c>).</returns>
    /// <exception cref="IOException">0install could not be launched or reported a problem accessing the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">0install reported that access to a resource was denied.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the operation.</exception>
    /// <exception cref="ExitCodeException">0install returned another error.</exception>
    Task<ISet<string>> GetIntegrationAsync(FeedUri uri, bool machineWide = false);

    /// <summary>
    /// Adds an application to the application list (if missing) and integrates it into the desktop environment.
    /// </summary>
    /// <param name="uri">The feed URI of the application.</param>
    /// <param name="add">The access point categories to add (e.g., <c>capability-registration</c>, <c>menu-entry</c>, <c>desktop-icon</c>).</param>
    /// <param name="remove">The access point categories to remove (e.g., <c>capability-registration</c>, <c>menu-entry</c>, <c>desktop-icon</c>).</param>
    /// <param name="machineWide">Apply the operation machine-wide instead of just for the current user.</param>
    /// <exception cref="NotAdminException"><paramref name="machineWide"/> was set but the current process is not running with admin rights.</exception>
    /// <exception cref="IOException">0install could not be launched or reported a problem accessing the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">0install reported that access to a resource was denied.</exception>
    /// <exception cref="WebException">0install reported a problem downloading a file.</exception>
    /// <exception cref="InvalidDataException">0install reported a problem parsing a file or an invalid signature.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the operation.</exception>
    /// <exception cref="ExitCodeException">0install returned another error.</exception>
    Task IntegrateAsync(FeedUri uri, IEnumerable<string>? add = null, IEnumerable<string>? remove = null, bool machineWide = false);

    /// <summary>
    /// Removes an application from the application list and undoes any desktop environment integration.
    /// </summary>
    /// <param name="uri">The feed URI of the application.</param>
    /// <param name="machineWide">Apply the operation machine-wide instead of just for the current user.</param>
    /// <exception cref="NotAdminException"><paramref name="machineWide"/> was set but the current process is not running with admin rights.</exception>
    /// <exception cref="IOException">0install could not be launched or reported a problem accessing the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">0install reported that access to a resource was denied.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the operation.</exception>
    /// <exception cref="ExitCodeException">0install returned another error.</exception>
    Task RemoveAsync(FeedUri uri, bool machineWide = false);

    /// <summary>
    /// Downloads a set of <see cref="Implementation"/>s.
    /// </summary>
    /// <param name="implementation">The implementations to download.</param>
    /// <exception cref="IOException">0install could not be launched or reported a problem accessing the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">0install reported that access to a resource was denied.</exception>
    /// <exception cref="WebException">0install reported a problem downloading a file.</exception>
    /// <exception cref="InvalidDataException">0install reported a problem parsing a file, an invalid signature or digest mismatch.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the operation.</exception>
    /// <exception cref="ExitCodeException">0install returned another error.</exception>
    Task FetchAsync(Implementation implementation);
}
