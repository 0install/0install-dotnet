// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
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
    /// Returns a command-line for executing the <c>0install run</c> command.
    /// Generates and returns a stub EXE if possible, falls back to directly pointing to the "0install" EXE otherwise.
    /// </summary>
    /// <param name="target">The application to be launched.</param>
    /// <param name="command">The command argument to be passed to the <c>0install run</c> command; can be <c>null</c>.</param>
    /// <param name="machineWide"><c>true</c> place the generated stub in a machine-wide location; <c>false</c> to place it in the current user profile.</param>
    /// <param name="needsTerminal"><c>true</c> if the sub should be a command-line app, <c>false</c> if it should be a GUI app, <c>null</c> if it should be auto-detected.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public IReadOnlyList<string> GetRunCommandLine(FeedTarget target, string? command, bool machineWide, bool? needsTerminal = null)
    {
        string targetKey = $"{target.Uri}#{command}";

        var entryPoint = target.Feed.GetEntryPoint(command);
        needsTerminal ??= entryPoint?.NeedsTerminal ?? false;

        string targetHash = targetKey.Hash(SHA256.Create());
        string exeName = (entryPoint == null)
            ? FeedUri.Escape(target.Feed.Name)
            : entryPoint.BinaryName ?? entryPoint.Command;
        string path = Paths.Combine(
            IntegrationManager.GetDir(machineWide, "stubs", targetHash),
            $"{exeName}.exe");

#if !DEBUG
        try
#endif
        {
            CreateOrUpdateRunStub(path, target, needsTerminal.Value, command);
            return [path];
        }
#if !DEBUG
        catch (Exception ex)
        {
            var exe = GetExe(needsTerminal.Value);
            Log.Error($"Failed to generate stub EXE for {targetKey}. Falling back to using '{exe}' directly.", ex);
            return GetArguments(target.Uri, command, needsTerminal.Value)
                  .Prepend(Paths.Combine(Locations.InstallBase, exe))
                  .ToList();
        }
#endif
    }

    private static string GetExe(bool needsTerminal)
        => needsTerminal ? "0install.exe" : "0install-win.exe";

    private static IEnumerable<string> GetArguments(FeedUri uri, string? command, bool needsTerminal)
    {
        yield return "run";
        if (!needsTerminal) yield return "--no-wait";
        if (!string.IsNullOrEmpty(command))
        {
            yield return "--command";
            yield return command;
        }
        yield return uri.ToStringRfc();
    }

    /// <summary>
    /// The revision of the stub template. Increment when changing the template to cause existing stubs to be rebuilt.
    /// </summary>
    /// <remarks>Stubs built by older versions of this library (compiled at runtime) contain no stub data and are treated as revision 0.</remarks>
    internal const int TemplateRevision = 1;

    /// <summary>The name of the <c>RT_RCDATA</c> resource containing the data read by the stub at runtime.</summary>
    private const string StubDataResourceName = "ZEROINSTALL_STUB";

    private void CreateOrUpdateRunStub(string path, FeedTarget target, bool needsTerminal, string? command)
    {
        if (File.Exists(path))
        { // Existing stub
            if (GetRevision(path) < TemplateRevision // Built by older version of this library, try to rebuild
             && !File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly)) // Don't try to overwrite readonly files
            {
                try
                {
                    BuildRunStub(path, target, command, needsTerminal);
                }
                catch (Exception ex)
                {
                    Log.Warn(string.Format(Resources.UnableToReplaceStub, path), ex);
                }
            }
        }
        else
        { // No existing stub, build new one
            BuildRunStub(path, target, command, needsTerminal);
        }
    }

    /// <summary>
    /// Determines the <see cref="TemplateRevision"/> an existing stub EXE was built with.
    /// </summary>
    /// <returns>The revision or 0 if the file is not a stub built from a template (e.g., compiled at runtime by an older version of this library).</returns>
    internal static int GetRevision(string path)
        => ReadStubData(path) is [var revision, ..]
        && int.TryParse(revision, NumberStyles.None, CultureInfo.InvariantCulture, out int result)
            ? result
            : 0;

    /// <summary>
    /// Reads the data fields (revision, exe, arguments, title) embedded in a stub EXE.
    /// </summary>
    /// <returns>The fields or <c>null</c> if the file is not a stub built from a template.</returns>
    internal static string[]? ReadStubData(string path)
        => Win32Resources.TryRead(path, Win32Resources.TypeRCData, StubDataResourceName)
                        ?.To(Encoding.Unicode.GetString)
                         .Split('\0');

    /// <summary>
    /// Builds a stub EXE that executes the <c>0install run</c> command at a specific path.
    /// </summary>
    /// <param name="path">The path to store the generated EXE file.</param>
    /// <param name="target">The application to be launched.</param>
    /// <param name="command">The command argument to be passed to the <c>0install run</c> command; can be <c>null</c>.</param>
    /// <param name="needsTerminal"><c>true</c> if the sub should be a command-line app, <c>false</c> if it should be a GUI app.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public void BuildRunStub(string path, FeedTarget target, string? command, bool needsTerminal)
        => BuildStub(
            path,
            exe: GetExe(needsTerminal),
            arguments: GetArguments(target.Uri, command, needsTerminal).JoinEscapeArguments(),
            title: target.Feed.GetBestName(CultureInfo.CurrentUICulture, command),
            needsTerminal,
            icon: GetIcon(target, command));

    /// <summary>
    /// Builds a stub EXE that executes a specific EXE at a specific path.
    /// </summary>
    /// <param name="path">The path to store the generated EXE file.</param>
    /// <param name="exe">The file name of the EXE to launch. Looked up in the Zero Install installation directory and the <c>PATH</c>.</param>
    /// <param name="arguments">The command-line arguments to pass to the EXE, followed by any arguments passed to the stub.</param>
    /// <param name="title">The title of the stub EXE, shown in Windows Explorer and Task Manager.</param>
    /// <param name="needsTerminal"><c>true</c> if the sub should be a command-line app, <c>false</c> if it should be a GUI app.</param>
    /// <param name="icon">The contents of an <c>.ico</c> file to use as the application icon; can be <c>null</c>.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    internal static void BuildStub(string path, string exe, string arguments, string title, bool needsTerminal, byte[]? icon = null)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        using var atomic = new AtomicWrite(path);
        typeof(StubBuilder).CopyEmbeddedToFile(needsTerminal ? "stub-cli.exe" : "stub-gui.exe", atomic.WritePath);
        Win32Resources.Update(atomic.WritePath, resources =>
        {
            resources.Set(Win32Resources.TypeRCData, StubDataResourceName,
                Encoding.Unicode.GetBytes(string.Join("\0", TemplateRevision.ToString(CultureInfo.InvariantCulture), exe, arguments, title)));
            resources.SetVersionInfo(
                ("FileDescription", title),
                ("ProductName", title),
                ("InternalName", Path.GetFileName(path)),
                ("OriginalFilename", Path.GetFileName(path)),
                ("FileVersion", "0.0.0.0"),
                ("ProductVersion", "0.0.0.0"));
            if (icon != null) resources.SetIcon(icon);
        });
        atomic.Commit();
    }

    private byte[]? GetIcon(FeedTarget target, string? command)
    {
        var icon = target.Feed.GetBestIcon(Icon.MimeTypeIco, command);
        if (icon == null) return null;

        try
        {
            var data = File.ReadAllBytes(iconStore.GetFresh(icon));
            Win32Resources.ParseIco(data); // Try to parse icon to ensure it is valid
            return data;
        }
        #region Error handling
        catch (Exception ex) when (ex is UriFormatException or WebException)
        {
            Log.Warn(ex);
        }
        catch (InvalidDataException ex)
        {
            Log.Warn($"Failed to parse {icon}", ex);
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
