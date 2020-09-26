// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using NanoByte.Common;
using NanoByte.Common.Storage;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Creates Windows shortcut files (.lnk).
    /// </summary>
    public static partial class Shortcut
    {
        /// <summary>
        /// Creates a new Windows shortcut.
        /// </summary>
        /// <param name="path">The location to place the shortcut at.</param>
        /// <param name="target">The target the shortcut shall point to.</param>
        /// <param name="command">The command within <paramref name="target"/> the shortcut shall point to; can be <c>null</c>.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        private static void Create(string path, FeedTarget target, string? command, IIconStore iconStore)
        {
            if (string.IsNullOrEmpty(command)) command = Command.NameRun;

            var entryPoint = target.Feed.GetEntryPoint(command);
            bool needsTerminal = (entryPoint != null && entryPoint.NeedsTerminal);

            string targetPath = Path.Combine(Locations.InstallBase, needsTerminal ? "0install.exe" : "0install-win.exe");

            string arguments = "run ";
            if (!needsTerminal) arguments += "--no-wait ";
            if (command != Command.NameRun) arguments += "--command " + command.EscapeArgument() + " ";
            arguments += target.Uri.ToStringRfc().EscapeArgument();

            var icon = target.Feed.GetBestIcon(Icon.MimeTypeIco, command);

            Create(path, targetPath, arguments,
                iconLocation: (icon == null) ? null : iconStore.GetPath(icon),
                description: target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, command));
        }

        /// <summary>
        /// Creates a new Windows shortcut.
        /// </summary>
        /// <param name="path">The location to place the shortcut at.</param>
        /// <param name="targetPath">The target path the shortcut shall point to.</param>
        /// <param name="arguments">Additional arguments to pass to the target; can be <c>null</c>.</param>
        /// <param name="iconLocation">The path of the icon to use for the shortcut; leave <c>null</c> ot get the icon from <paramref name="targetPath"/>.</param>
        /// <param name="description">A short human-readable description; can be <c>null</c>.</param>
        public static void Create(string path, string targetPath, string? arguments = null, string? iconLocation = null, string? description = null)
        {
            if (File.Exists(path)) File.Delete(path);

            // ReSharper disable once SuspiciousTypeConversion.Global
            var link = (IShellLink)new ShellLink();

            link.SetPath(targetPath);
            if (!string.IsNullOrEmpty(arguments)) link.SetArguments(arguments);
            if (!string.IsNullOrEmpty(iconLocation)) link.SetIconLocation(iconLocation, 0);
            if (!string.IsNullOrEmpty(description)) link.SetDescription(description.Substring(0, Math.Min(description.Length, 256)));

            // ReSharper disable once SuspiciousTypeConversion.Global
            ((IPersistFile)link).Save(path, fRemember: false);
        }

        /// <summary>
        /// Ensures that the given name can be used as a file name.
        /// </summary>
        /// <exception cref="IOException"><paramref name="name"/> contains invalid characters.</exception>
        private static void CheckName(string? name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                throw new IOException(string.Format(Resources.NameInvalidChars, name));
        }
    }
}
