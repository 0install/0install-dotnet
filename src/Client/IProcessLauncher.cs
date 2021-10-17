// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.IO;
using NanoByte.Common;

namespace ZeroInstall.Client
{
    /// <summary>
    /// Launches an external process.
    /// </summary>
    internal interface IProcessLauncher
    {
        /// <summary>
        /// Starts a new <see cref="Process"/> and runs it in parallel with this one.
        /// </summary>
        /// <returns>The newly launched process.</returns>
        /// <exception cref="IOException">There was a problem launching the executable.</exception>
        /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
        /// <exception cref="NotAdminException">The target process requires elevation.</exception>
        Process Start(params string[] args);

        /// <summary>
        /// Starts a new <see cref="Process"/> and waits for it to complete.
        /// </summary>
        /// <returns>The exit code of the child process.</returns>
        /// <exception cref="IOException">There was a problem launching the executable.</exception>
        /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
        /// <exception cref="NotAdminException">The target process requires elevation.</exception>
        int Run(params string[] args);
    }
}
