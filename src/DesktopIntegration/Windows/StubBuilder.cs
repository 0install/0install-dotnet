// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Builds stub EXEs that execute "0install" commands.
/// </summary>
[SupportedOSPlatform("windows")]
public class StubBuilder(IIconStore iconStore)
{
    /// <summary>
    /// Returns a command-line for executing the "0install run" command.
    /// Generates and returns a stub EXE if possible, falls back to directly pointing to the "0install" EXE otherwise.
    /// </summary>
    /// <param name="target">The application to be launched.</param>
    /// <param name="command">The command argument to be passed to the the "0install run" command; can be <c>null</c>.</param>
    /// <param name="machineWide"><c>true</c> place the generated stub in a machine-wide location; <c>false</c> to place it in the current user profile.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="InvalidOperationException">There was a compilation error while generating the stub EXE.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public IReadOnlyList<string> GetRunCommandLine(FeedTarget target, string? command = null, bool machineWide = false)
    {
        string targetKey = $"{target.Uri}#{command}";

        var entryPoint = target.Feed.GetEntryPoint(command);
        bool gui = entryPoint is not {NeedsTerminal: true};

        string targetHash = targetKey.Hash(SHA256.Create());
        string exeName = (entryPoint == null)
            ? FeedUri.Escape(target.Feed.Name)
            : entryPoint.BinaryName ?? entryPoint.Command;
        string path = Path.Combine(
            IntegrationManager.GetDir(machineWide, "stubs", targetHash),
            $"{exeName}.exe");

#if !DEBUG
        try
#endif
        {
            CreateOrUpdateRunStub(path, target, gui, command);
            return [path];
        }
#if !DEBUG
        catch (Exception ex)
        {
            var exe = GetExe(gui);
            Log.Error($"Failed to generate stub EXE for {targetKey}. Falling back to using '{exe}' directly.", ex);
            return GetArguments(target.Uri, command, gui)
                  .Prepend(Path.Combine(Locations.InstallBase, exe))
                  .ToList();
        }
#endif
    }

    private static string GetExe(bool gui)
        => gui ? "0install-win.exe" : "0install.exe";

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

    /// <summary>The point in time when the stub template was last changed.</summary>
    private static readonly DateTime _templateLastChanged = new(2023,5, 3, 12, 0, 0, DateTimeKind.Utc);

    private void CreateOrUpdateRunStub(string path, FeedTarget target, bool gui, string? command)
    {
        if (File.Exists(path))
        { // Existing stub
            if (File.GetLastWriteTimeUtc(path) < _templateLastChanged)
            { // Built by older version of this library, try to rebuild
                try
                {
                    BuildRunStub(path, target, command, gui);
                }
                catch (Exception ex)
                {
                    Log.Warn(string.Format(Resources.UnableToReplaceStub, path), ex);
                }
            }
        }
        else
        { // No existing stub, build new one
            BuildRunStub(path, target, command, gui);
        }
    }

    private static readonly string _netFxDirectory = WindowsUtils.GetNetFxDirectory(WindowsUtils.NetFx40);

    private static readonly IEnumerable<PortableExecutableReference> _references =
        new[] {"mscorlib.dll", "System.dll", "System.Core.dll"}
           .Select(x => MetadataReference.CreateFromFile(Path.Combine(_netFxDirectory, x)))
           .ToArray();

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

        var compilation = CSharpCompilation.Create(
            assemblyName: "ZeroInstall.Stub",
            syntaxTrees:
            [
                GetCode(
                    exe: GetExe(gui),
                    arguments: GetArguments(target.Uri, command, gui),
                    title: target.Feed.GetBestName(CultureInfo.CurrentUICulture, command))
            ],
            _references,
            options: new(
                gui ? OutputKind.WindowsApplication : OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                deterministic: true));
        var resources = GetResources(compilation, GetIconPath(target, command));

        using var atomic = new AtomicWrite(path);
        using (var stream = File.Create(atomic.WritePath))
        {
            var result = compilation.Emit(stream, win32Resources: resources);
            if (!result.Success)
                throw new IOException(result.Diagnostics.FirstOrDefault()?.ToString());
        }
        atomic.Commit();
    }

    private static SyntaxTree GetCode(string exe, IEnumerable<string> arguments, string title)
        => CSharpSyntaxTree.ParseText(
            typeof(StubBuilder)
               .GetEmbeddedString("stub.template.cs")
               .Replace("[EXE]", EscapeForCode(exe))
               .Replace("[ARGUMENTS]", EscapeForCode(arguments.JoinEscapeArguments()))
               .Replace("[TITLE]", EscapeForCode(title)));

    private static string EscapeForCode(string value)
        => value.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("\n", @"\n");

    private static Stream GetResources(Compilation compilation, string? iconPath)
    {
        using var manifestStream = typeof(StubBuilder).GetEmbeddedStream("Stub.manifest");
        using var iconStream = iconPath?.To(File.OpenRead);
        return compilation.CreateDefaultWin32Resources(
            versionResource: true,
            noManifest: false,
            manifestStream,
            iconStream);
    }

    private string? GetIconPath(FeedTarget target, string? command)
    {
        var icon = target.Feed.GetBestIcon(Icon.MimeTypeIco, command);
        if (icon == null) return null;

        try
        {
            string iconPath = iconStore.GetFresh(icon);
#if NETFRAMEWORK
            new System.Drawing.Icon(iconPath).Dispose(); // Try to parse icon to ensure it is valid
#endif
            return iconPath;
        }
        #region Error handling
        catch (Exception ex) when (ex is UriFormatException or WebException)
        {
            Log.Warn(ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Failed to store {icon}", ex);
        }
        catch (ArgumentException ex)
        {
            Log.Warn($"Failed to parse {icon}", ex);
        }
        #endregion

        return null;
    }
}
