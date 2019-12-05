// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Drawing;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// An executable with embedded icons.
    /// </summary>
    public interface IIconContainer
    {
#if NETFRAMEWORK
        /// <summary>
        /// Extracts the primary icon of the executable.
        /// </summary>
        Icon ExtractIcon();
#endif
    }
}
