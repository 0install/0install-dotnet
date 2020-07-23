// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Builds stub EXEs that execute "0install" commands.
    /// </summary>
    internal class StubBuilder
    {
        private readonly IIconStore _iconStore;

        /// <summary>
        /// Creates a new stub builder.
        /// </summary>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        public StubBuilder(IIconStore iconStore)
        {
            _iconStore = iconStore;
        }

        /// <summary>
        /// Returns a command-line for executing the "0install run" command. Generates and returns a stub EXE if possible, falls back to directly pointing to the "0install" binary otherwise.
        /// </summary>
        /// <param name="target">The application to be launched.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
        /// <param name="machineWide"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="InvalidOperationException">There was a compilation error while generating the stub EXE.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        public IReadOnlyList<string> GetRunCommandLine(FeedTarget target, string? command = null, bool machineWide = false)
        {
            var entryPoint = target.Feed.GetEntryPoint(command);
            bool gui = entryPoint == null || !entryPoint.NeedsTerminal;

            try
            {
                if (WindowsUtils.IsWindows)
                {
                    string hash = (target.Uri + "#" + command).Hash(SHA256.Create());
                    string exeName = (entryPoint == null)
                        ? FeedUri.Escape(target.Feed.Name)
                        : entryPoint.BinaryName ?? entryPoint.Command;
                    string path = Path.Combine(
                        IntegrationManager.GetDir(machineWide, "stubs", hash),
                        exeName + ".exe");

                    CreateOrUpdateRunStub(path, target, gui, command);
                    return new[] {path};
                }

                return GetArguments(target.Uri, command, gui)
                      .Prepend(GetBinary(gui))
                      .ToList();
            }
            #region Error handling
            catch (InvalidOperationException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        }

        private static string GetBinary(bool gui)
            => Path.Combine(Locations.InstallBase, gui ? "0install-win.exe" : "0install.exe");

        private static IEnumerable<string> GetArguments(FeedUri uri, string? command, bool gui)
        {
            yield return "run";
            if (gui) yield return "--no-wait";
            if (!string.IsNullOrEmpty(command))
            {
                yield return "--command";
                yield return command;
            }
            yield return uri.ToStringRfc();
        }

        /// <summary>The point in time when the library file containing this code was installed.</summary>
        private static readonly DateTime _libraryInstallTimestamp = File.GetCreationTimeUtc(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

        private void CreateOrUpdateRunStub(string path, FeedTarget target, bool gui, string? command)
        {
            if (File.Exists(path))
            { // Existing stub
                if (File.GetLastWriteTimeUtc(path) < _libraryInstallTimestamp)
                { // Built by older version of this library, try to rebuild
                    try
                    {
                        File.Delete(path);
                    }
                    #region Error handling
                    catch (IOException ex)
                    {
                        Log.Warn(string.Format(Resources.UnableToReplaceStub, path));
                        Log.Warn(ex);
                        return;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warn(string.Format(Resources.UnableToReplaceStub, path));
                        Log.Warn(ex);
                        return;
                    }
                    #endregion

                    BuildRunStub(path, target, command, gui);
                }
            }
            else
            { // No existing stub, build new one
                BuildRunStub(path, target, command, gui);
            }
        }

        /// <summary>
        /// Builds a stub EXE that executes the "0install run" command at a specific path.
        /// </summary>
        /// <param name="path">The path to store the generated EXE file.</param>
        /// <param name="target">The application to be launched.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
        /// <param name="gui"><c>true</c> to build a GUI stub, <c>false</c> to build a CLI stub.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="InvalidOperationException">There was a compilation error while generating the stub EXE.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        public void BuildRunStub(string path, FeedTarget target, string? command = null, bool gui = false)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            var compilerParameters = new CompilerParameters
            {
                OutputAssembly = path,
                GenerateExecutable = true,
                TreatWarningsAsErrors = true,
                ReferencedAssemblies = {"System.dll"},
                CompilerOptions = gui ? "/target:winexe" : "/target:exe"
            };

            string? iconPath = GetIconPath(target, command);
            if (iconPath != null)
                compilerParameters.CompilerOptions += " /win32icon:" + iconPath.EscapeArgument();

            compilerParameters.CompileCSharp(
                GetCode(
                    exe: GetBinary(gui),
                    arguments: GetArguments(target.Uri, command, gui),
                    title: target.Feed.GetBestName(CultureInfo.CurrentUICulture, command)),
                Manifest);
        }

        private string? GetIconPath(FeedTarget target, string? command)
        {
            var icon = target.Feed.GetIcon(Icon.MimeTypeIco, command);
            if (icon == null) return null;

            try
            {
                string iconPath = _iconStore.GetPath(icon);
                new System.Drawing.Icon(iconPath).Dispose(); // Try to parse icon to ensure it is valid
                return iconPath;
            }
            #region Error handling
            catch (UriFormatException ex)
            {
                Log.Warn(ex);
            }
            catch (WebException ex)
            {
                Log.Warn(ex);
            }
            catch (IOException ex)
            {
                Log.Warn($"Failed to store {icon}");
                Log.Warn(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn($"Failed to store {icon}");
                Log.Warn(ex);
            }
            catch (ArgumentException ex)
            {
                Log.Warn($"Failed to parse {icon}");
                Log.Warn(ex);
            }
            #endregion

            return null;
        }

        private static string GetCode(string exe, IEnumerable<string> arguments, string title)
            => typeof(StubBuilder)
              .GetEmbeddedString("stub.template.cs")
              .Replace("[EXE]", EscapeForCode(exe))
              .Replace("[ARGUMENTS]", EscapeForCode(arguments.JoinEscapeArguments()))
              .Replace("[TITLE]", EscapeForCode(title));

        private static string Manifest
            => typeof(StubBuilder).GetEmbeddedString("Stub.manifest");

        private static string EscapeForCode(string value)
            => value.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("\n", @"\n");
    }
}
