// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System.Drawing;
#endif

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
