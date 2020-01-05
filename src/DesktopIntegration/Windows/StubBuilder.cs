// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Utility class for building stub EXEs that execute "0install" commands. Provides persistent local paths.
    /// </summary>
    public static class StubBuilder
    {
        #region Get
        /// <summary>
        /// Builds a stub EXE in a well-known location. Future calls with the same arguments will return the same EXE.
        /// </summary>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
        /// <param name="machineWide">Store the stub in a machine-wide directory instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <returns>The path to the generated stub EXE.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="InvalidOperationException">There was a compilation error while generating the stub EXE.</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="InvalidOperationException">Write access to the filesystem is not permitted.</exception>
        public static string GetRunStub(FeedTarget target, string? command, IIconStore iconStore, bool machineWide = false)
        {
            #region Sanity checks
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var entryPoint = target.Feed.GetEntryPoint(command);
            string exeName = (entryPoint != null)
                ? entryPoint.BinaryName ?? entryPoint.Command
                : FeedUri.Escape(target.Feed.Name);
            bool needsTerminal = (entryPoint != null && entryPoint.NeedsTerminal);

            string hash = (target.Uri + "#" + command).Hash(SHA256.Create());
            string path = Path.Combine(Locations.GetIntegrationDirPath("0install.net", machineWide, "desktop-integration", "stubs", hash), exeName + ".exe");

            CreateOrUpdateRunStub(target, path, command, needsTerminal, iconStore);
            return path;
        }

        /// <summary>The point in time when the library file containing this code was installed.</summary>
        private static readonly DateTime _libraryInstallTimestamp = File.GetCreationTimeUtc(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

        /// <summary>
        /// Creates a new or updates an existing stub EXE that executes the "0install run" command.
        /// </summary>
        /// <seealso cref="BuildRunStub"/>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="path">The target path to store the generated EXE file.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
        /// <param name="needsTerminal"><c>true</c> to build a CLI stub, <c>false</c> to build a GUI stub.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="InvalidOperationException">There was a compilation error while generating the stub EXE.</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        private static void CreateOrUpdateRunStub(FeedTarget target, string path, string? command, bool needsTerminal, IIconStore iconStore)
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

                    BuildRunStub(target, path, iconStore, needsTerminal, command);
                }
            }
            else
            { // No existing stub, build new one
                BuildRunStub(target, path, iconStore, needsTerminal, command);
            }
        }
        #endregion

        #region Build
        /// <summary>
        /// Builds a stub EXE that executes the "0install run" command.
        /// </summary>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="path">The target path to store the generated EXE file.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="needsTerminal"><c>true</c> to build a CLI stub, <c>false</c> to build a GUI stub.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="InvalidOperationException">There was a compilation error while generating the stub EXE.</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        internal static void BuildRunStub(FeedTarget target, string path, IIconStore iconStore, bool needsTerminal, string? command = null)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            var compilerParameters = new CompilerParameters
            {
                OutputAssembly = path,
                GenerateExecutable = true,
                TreatWarningsAsErrors = true,
                ReferencedAssemblies = {"System.dll"},
                CompilerOptions = needsTerminal ? "/target:exe" : "/target:winexe"
            };

            var icon = target.Feed.GetIcon(Icon.MimeTypeIco, command);
            if (icon != null)
            {
                try
                {
                    string iconPath = iconStore.GetPath(icon);
                    new System.Drawing.Icon(iconPath).Dispose(); // Try to parse icon to ensure it is valid
                    compilerParameters.CompilerOptions += " /win32icon:" + iconPath.EscapeArgument();
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
            }

            compilerParameters.CompileCSharp(
                code: GetRunStubCode(target, needsTerminal, command),
                manifest: typeof(StubBuilder).GetEmbeddedString("Stub.manifest"));
        }

        /// <summary>
        /// Generates the C# to be compiled for the stub EXE.
        /// </summary>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="needsTerminal"><c>true</c> to build a CLI stub, <c>false</c> to build a GUI stub.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
        /// <returns>Generated C# code.</returns>
        private static string GetRunStubCode(FeedTarget target, bool needsTerminal, string? command = null)
        {
            // Build command-line
            string args = needsTerminal ? "run " : "run --no-wait ";
            if (!string.IsNullOrEmpty(command)) args += "--command " + command.EscapeArgument() + " ";
            args += target.Uri.ToStringRfc().EscapeArgument();

            // Load the template code and insert variables
            return typeof(StubBuilder)
                  .GetEmbeddedString("stub.template.cs")
                  .Replace("[EXE]", Path.Combine(Locations.InstallBase, needsTerminal ? "0install.exe" : "0install-win.exe").Replace(@"\", @"\\"))
                  .Replace("[ARGUMENTS]", EscapeForCode(args))
                  .Replace("[TITLE]", EscapeForCode(target.Feed.GetBestName(CultureInfo.CurrentUICulture, command)));
        }

        /// <summary>
        /// Escapes a string so that is safe for substitution inside C# code
        /// </summary>
        private static string EscapeForCode(string value) => value.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("\n", @"\n");
        #endregion
    }
}
