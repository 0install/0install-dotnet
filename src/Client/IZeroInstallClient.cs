// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Client
{
    /// <summary>
    /// Client for invoking Zero Install commands from within other applications.
    /// </summary>
    public interface IZeroInstallClient
    {
        /// <summary>
        /// Selects a program and compatible versions of all of its dependencies.
        /// </summary>
        /// <param name="requirements">The requirements describing the program.</param>
        /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
        /// <param name="offline">Do not refresh feeds even if they are out-of-date and don't select newer versions of programs for downloading even if they are already known.</param>
        /// <returns>The selected implementations.</returns>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        /// <exception cref="InvalidOperationException">The external process returned a non-zero exit code.</exception>
        Task<Selections> SelectAsync(Requirements requirements, bool refresh = false, bool offline = false);

        /// <summary>
        /// Downloads a program and compatible versions of all of its dependencies.
        /// </summary>
        /// <param name="requirements">The requirements describing the program.</param>
        /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
        /// <returns>The downloaded implementations.</returns>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        /// <exception cref="InvalidOperationException">The external process returned a non-zero exit code.</exception>
        Task<Selections> DownloadAsync(Requirements requirements, bool refresh = false);

        /// <summary>
        /// Runs a program via Zero Install.
        /// </summary>
        /// <param name="requirements">The requirements describing the program.</param>
        /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
        /// <param name="needsTerminal">Indicates that the program requires a terminal in order to run.</param>
        /// <param name="arguments">Additional arguments to pass to the program.</param>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        void Run(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments);

        /// <summary>
        /// Runs a program via Zero Install and returns the process.
        /// </summary>
        /// <param name="requirements">The requirements describing the program.</param>
        /// <param name="refresh">Fetch fresh copies of all used feeds.</param>
        /// <param name="needsTerminal">Indicates that the program requires a terminal in order to run.</param>
        /// <param name="arguments">Additional arguments to pass to the program.</param>
        /// <returns>The newly launched process.</returns>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        /// <exception cref="InvalidOperationException">The external process returned a non-zero exit code.</exception>
        Process RunWithProcess(Requirements requirements, bool refresh = false, bool needsTerminal = false, params string[] arguments);

        /// <summary>
        /// Returns the desktop integration categories that are currently applied for a specific feed.
        /// </summary>
        /// <param name="uri">The feed URI of the application.</param>
        /// <returns>The access point categories (e.g., <c>capability-registration</c>, <c>menu-entry</c>, <c>desktop-icon</c>).</returns>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        /// <exception cref="InvalidOperationException">The external process returned a non-zero exit code.</exception>
        Task<ISet<string>> GetIntegrationAsync(FeedUri uri);

        /// <summary>
        /// Adds an application to the application list (if missing) and integrates it into the desktop environment.
        /// </summary>
        /// <param name="uri">The feed URI of the application.</param>
        /// <param name="add">The access point categories to add (e.g., <c>capability-registration</c>, <c>menu-entry</c>, <c>desktop-icon</c>).</param>
        /// <param name="remove">The access point categories to remove (e.g., <c>capability-registration</c>, <c>menu-entry</c>, <c>desktop-icon</c>).</param>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        /// <exception cref="InvalidOperationException">The external process returned a non-zero exit code.</exception>
        Task IntegrateAsync(FeedUri uri, IEnumerable<string>? add = null, IEnumerable<string>? remove = null);

        /// <summary>
        /// Removes an application from the application list and undoes any desktop environment integration.
        /// </summary>
        /// <param name="uri">The feed URI of the application.</param>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        /// <exception cref="InvalidOperationException">The external process returned a non-zero exit code.</exception>
        Task RemoveAsync(FeedUri uri);

        /// <summary>
        /// Downloads a set of <see cref="Implementation"/>s.
        /// </summary>
        /// <param name="implementation">The implementations to download.</param>
        /// <exception cref="IOException">The external process could not be launched.</exception>
        /// <exception cref="InvalidOperationException">The external process returned a non-zero exit code.</exception>
        Task FetchAsync(Implementation implementation);
    }
}
