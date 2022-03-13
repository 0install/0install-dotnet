// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System.CodeDom.Compiler;
using System.Reflection;
using System.Security.Cryptography;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
#endif

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Builds stub EXEs that execute "0install" commands.
/// </summary>
[PrimaryConstructor]
public partial class StubBuilder
{
    // ReSharper disable once NotAccessedField.Local
    private readonly IIconStore _iconStore;

    /// <summary>
    /// Returns a command-line for executing the "0install run" command. Generates and returns a stub EXE if possible, falls back to directly pointing to the "0install" binary otherwise.
    /// </summary>
    /// <param name="target">The application to be launched.</param>
    /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
    /// <param name="machineWide"><c>true</c> place the generated stub in a machine-wide location; <c>false</c> to place it in the current user profile.</param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="InvalidOperationException">There was a compilation error while generating the stub EXE.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public IReadOnlyList<string> GetRunCommandLine(FeedTarget target, string? command = null, bool machineWide = false)
    {
        var entryPoint = target.Feed.GetEntryPoint(command);
        bool gui = entryPoint is not {NeedsTerminal: true};

        try
        {
#if NETFRAMEWORK
            string hash = (target.Uri + "#" + command).Hash(SHA256.Create());
            string exeName = (entryPoint == null)
                ? FeedUri.Escape(target.Feed.Name)
                : entryPoint.BinaryName ?? entryPoint.Command;
            string path = Path.Combine(
                IntegrationManager.GetDir(machineWide, "stubs", hash),
                exeName + ".exe");

            CreateOrUpdateRunStub(path, target, gui, command);
            return new[] {path};
#else
                return GetArguments(target.Uri, command, gui)
                      .Prepend(GetBinary(gui))
                      .ToList();
#endif
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

#if NETFRAMEWORK
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
                    BuildRunStub(path, target, command, gui);
                }
                catch (Exception ex)
                {
                    Log.Warn(string.Format(Resources.UnableToReplaceStub, path));
                    Log.Warn(ex);
                }
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

        using var atomic = new AtomicWrite(path);

        var compilerParameters = new CompilerParameters
        {
            OutputAssembly = atomic.WritePath,
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

        atomic.Commit();
    }

    private string? GetIconPath(FeedTarget target, string? command)
    {
        var icon = target.Feed.GetBestIcon(Icon.MimeTypeIco, command);
        if (icon == null) return null;

        try
        {
            string iconPath = _iconStore.GetFresh(icon);
            new System.Drawing.Icon(iconPath).Dispose(); // Try to parse icon to ensure it is valid
            return iconPath;
        }
        #region Error handling
        catch (Exception ex) when (ex is UriFormatException or WebException)
        {
            Log.Warn(ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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
#endif
}
