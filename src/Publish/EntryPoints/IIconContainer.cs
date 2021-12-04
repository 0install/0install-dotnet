// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Drawing;
using System.Runtime.Versioning;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// An executable with embedded icons.
    /// </summary>
    public interface IIconContainer
    {
        /// <summary>
        /// Extracts the primary icon of the executable.
        /// </summary>
        [SupportedOSPlatform("windows")]
        Icon ExtractIcon();
    }
}
