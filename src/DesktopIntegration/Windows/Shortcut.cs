// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using NanoByte.Common;
using NanoByte.Common.Storage;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Model;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;

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
            bool needsTerminal = entryPoint is {NeedsTerminal: true};

            string targetPath = Path.Combine(Locations.InstallBase, needsTerminal ? "0install.exe" : "0install-win.exe");

            string arguments = "run ";
            if (!needsTerminal) arguments += "--no-wait ";
            if (command != Command.NameRun) arguments += "--command " + command.EscapeArgument() + " ";
            arguments += target.Uri.ToStringRfc().EscapeArgument();

            var icon = target.Feed.GetBestIcon(Icon.MimeTypeIco, command);

            Create(path, targetPath, arguments,
                iconLocation: (icon == null) ? null : iconStore.GetPath(icon),
                description: target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, command),
                appId: entryPoint?.AppId ?? GuessAppExePath(target.Feed, entryPoint));
        }

        private static string? GuessAppExePath(Feed feed, EntryPoint? entryPoint)
        {
            if (entryPoint == null || string.IsNullOrEmpty(entryPoint.BinaryName)) return null;

            string? referenceDigest =
                feed.Implementations
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault(x => x.Architecture.RunsOn(Architecture.CurrentSystem))
                   ?.ManifestDigest.Best;
            if (referenceDigest == null) return null;

            return Path.Combine(ImplementationStores.GetDirectories().Last(), referenceDigest, entryPoint.BinaryName + ".exe");
        }

        /// <summary>
        /// Creates a new Windows shortcut.
        /// </summary>
        /// <param name="path">The location to place the shortcut at.</param>
        /// <param name="targetPath">The target path the shortcut shall point to.</param>
        /// <param name="arguments">Additional arguments to pass to the target; can be <c>null</c>.</param>
        /// <param name="iconLocation">The path of the icon to use for the shortcut; leave <c>null</c> ot get the icon from <paramref name="targetPath"/>.</param>
        /// <param name="description">A short human-readable description; can be <c>null</c>.</param>
        /// <param name="appId">The Application User Model ID; used by Windows to associate shortcuts and pinned taskbar entries with running processes.</param>
        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "COM interfaces")]
        public static void Create(string path, string targetPath, string? arguments = null, string? iconLocation = null, string? description = null, string? appId = null)
        {
            if (File.Exists(path)) File.Delete(path);

            var link = (IShellLink)new ShellLink();
            link.SetPath(targetPath);
            if (!string.IsNullOrEmpty(arguments)) link.SetArguments(arguments);
            if (!string.IsNullOrEmpty(iconLocation)) link.SetIconLocation(iconLocation, 0);
            if (!string.IsNullOrEmpty(description)) link.SetDescription(description[..Math.Min(description.Length, 256)]);

            if (!string.IsNullOrEmpty(appId))
                ((IPropertyStore)link).SetValue(PropertyKey.AppUserModelID, appId);

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
