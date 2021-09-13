// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows
{
    public static partial class Shortcut
    {
        /// <summary>
        /// Creates a new Windows shortcut in the "Send to" menu.
        /// </summary>
        /// <param name="sendTo">Information about the shortcut to be created.</param>
        /// <param name="target">The target the shortcut shall point to.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        public static void Create(SendTo sendTo, FeedTarget target, IIconStore iconStore)
        {
            #region Sanity checks
            if (sendTo == null) throw new ArgumentNullException(nameof(sendTo));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            string filePath = GetSendToPath(sendTo.Name);
            Create(filePath, target, sendTo.Command, iconStore);
        }

        /// <summary>
        /// Removes a Windows shortcut from the "Send to" menu.
        /// </summary>
        /// <param name="sendTo">Information about the shortcut to be removed.</param>
        public static void Remove(SendTo sendTo)
        {
            #region Sanity checks
            if (sendTo == null) throw new ArgumentNullException(nameof(sendTo));
            #endregion

            string filePath = GetSendToPath(sendTo.Name);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        private static string GetSendToPath(string? name)
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SendTo), name + ".lnk");
    }
}
