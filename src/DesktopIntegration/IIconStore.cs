/*
 * Copyright 2010-2018 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Stores icon files downloaded from the web as local files.
    /// </summary>
    public interface IIconStore
    {
        /// <summary>
        /// Gets a specific icon from this cache. If the icon is missing it will be downloaded.
        /// </summary>
        /// <param name="icon">The icon to retrieve.</param>
        /// <param name="machineWide">Use a machine-wide cache directory instead of one just for the current user.</param>
        /// <returns>The local file path of the cached icon.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
        /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
        [NotNull]
        string GetPath([NotNull] Icon icon, bool machineWide = false);
    }
}
