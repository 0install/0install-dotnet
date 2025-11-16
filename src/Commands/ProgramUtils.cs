// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Security;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Values;
using ZeroInstall.Commands.Desktop;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

#if NETFRAMEWORK
using System.Reflection;
using NanoByte.Common.Streams;
#endif

namespace ZeroInstall.Commands;

/// <summary>
/// Provides utility methods for application entry points.
/// </summary>
public static class ProgramUtils
{
    /// <summary>
    /// The current UI language; <c>null</c> to use system default.
    /// </summary>
    /// <remarks>This value is only used on Windows and is stored in the Registry. For non-Windows platforms use the <c>LC_*</c> environment variables instead.</remarks>
    public static CultureInfo? UILanguage
    {
        get
        {
            if (!WindowsUtils.IsWindows) return null;

            string? language = RegistryUtils.GetSoftwareString("Zero Install", "Language", machineWide: false)
                            ?? RegistryUtils.GetSoftwareString("Zero Install", "Language", machineWide: true);
            if (string.IsNullOrEmpty(language)) return null;

            try
            {
                return Languages.FromString(language);
            }
            catch (ArgumentException ex)
            {
                Log.Warn($"Failed to parse '{language}' as an ISO language code", ex);
                return null;
            }
        }
        set
        {
            if (WindowsUtils.IsWindows)
                RegistryUtils.SetSoftwareString("Zero Install", "Language", value?.ToString() ?? "");
        }
    }

    /// <summary>
    /// Common initialization code to be called by every Zero Install executable right after startup.
    /// </summary>
    public static void Init()
    {
        AppMutex.Create(ZeroInstallEnvironment.MutexName());
        AppMutex.Create(ZeroInstallEnvironment.LegacyMutexName());
        if (AppMutex.Probe(ZeroInstallEnvironment.UpdateMutexName()) || AppMutex.Probe(ZeroInstallEnvironment.LegacyUpdateMutexName())) Environment.Exit(999);

        ReplaceMissingRuntimeConfig();

        if (UILanguage != null) Languages.SetUI(UILanguage);

        ProcessUtils.SanitizeEnvironmentVariables();
        NetUtils.ApplyProxy();
#if !NET9_0_OR_GREATER
        ServicePointManager.DefaultConnectionLimit = 16;
#endif
    }

    private static void ReplaceMissingRuntimeConfig()
    {
#if NETFRAMEWORK
        string path = Assembly.GetEntryAssembly()!.Location + ".config";
        if (File.Exists(path)) return;

        Log.Info($"Replacing missing runtime config file: {path}");
        try
        {
            typeof(ProgramUtils).CopyEmbeddedToFile("0install.exe.config", path);
        }
        #region Error handling
        catch (Exception ex)
        {
            Log.Error("Failed to replace missing runtime config file", ex);
        }
        #endregion
#endif
    }

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> for launching an instance of the 0install command-line interface.
    /// </summary>
    public static ProcessStartInfo? CliStartInfo(params string[] arguments)
    {
        try
        {
            return ProcessUtils.Assembly("0install", arguments);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> for launching an instance of the 0install graphical interface.
    /// </summary>
    public static ProcessStartInfo? GuiStartInfo(params string[] arguments)
    {
        try
        {
            return WindowsUtils.IsWindows && WindowsUtils.IsGuiSession
                ? ProcessUtils.Assembly("0install-win", arguments)
                : null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private const string
        RegKeyFSPolicyMachine = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem",
        RegKeyFSPolicyUser = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy Objects\{B0D05113-7B6B-4D69-81E2-8E8836775C9C}Machine\System\CurrentControlSet\Control\FileSystem",
        RegValueNameLongPaths = "LongPathsEnabled";

    /// <summary>A command-line argument used to indicate that the program was relaunched with admin rights.</summary>
    private const string AsAdminIndicatorArg = "--as-admin";

    /// <summary>A command-line argument used to indicate that the program was relaunched as a freshly deployed instance.</summary>
    private const string DeployedIndicatorArg = "--deployed";

    /// <summary>
    /// Parses command-line arguments and performs the indicated action. Performs error handling.
    /// </summary>
    /// <param name="exeName">The name of the executable to use as a reference in help messages and self-invocation.</param>
    /// <param name="args">The arguments to be processed.</param>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <returns>The exit status code to end the process with. Cast to <see cref="int"/> to return from a Main method.</returns>
    public static ExitCode Run(string exeName, string[] args, ICommandHandler handler)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(exeName)) throw new ArgumentNullException(nameof(exeName));
        if (args == null) throw new ArgumentNullException(nameof(args));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        if (args is [AsAdminIndicatorArg or DeployedIndicatorArg, ..])
            args = args.Skip(1).ToArray();

        try
        {
            var command = CliCommand.CreateAndParse(args, handler);
            return command.Execute();
        }
        #region Error handling
        catch (OperationCanceledException)
        {
            return ExitCode.UserCanceled;
        }
        catch (NeedsGuiException) when (GuiStartInfo(args) is {} startInfo)
        {
            Log.Info("Switching to GUI");
            handler.DisableUI();
            try
            {
                return (ExitCode)startInfo.Run();
            }
            catch (IOException ex2)
            {
                handler.Error(ex2);
                return ExitCode.IOError;
            }
            catch (NotAdminException ex2)
            {
                handler.Error(ex2);
                return ExitCode.AccessDenied;
            }
        }
        catch (NotAdminException ex) when (WindowsUtils.HasUac
                                        && args.FirstOrDefault() != AsAdminIndicatorArg
                                        && GuiStartInfo(args.Prepend(AsAdminIndicatorArg)) is {} startInfo)
        {
            Log.Info("Elevating to admin", ex);
            handler.DisableUI();
            try
            {
                return (ExitCode)startInfo.AsAdmin().Run();
            }
            catch (IOException ex2)
            {
                handler.Error(ex2);
                return ExitCode.IOError;
            }
            catch (OperationCanceledException)
            {
                return ExitCode.UserCanceled;
            }
        }
        catch (ConflictException ex)
        {
            handler.Error(ex);
            return ExitCode.Conflict;
        }
        catch (UnsuitableInstallBaseException ex) when (WindowsUtils.IsWindows && args.FirstOrDefault() != DeployedIndicatorArg)
        {
            Log.Info(ex.Message, ex);

            try
            {
                return TryRunOtherInstance(exeName, args, handler, ex.NeedsMachineWide)
                    ?? DeployAndRunOtherInstance(exeName, args, handler, ex.NeedsMachineWide);
            }
            catch (OperationCanceledException)
            {
                return ExitCode.UserCanceled;
            }
            catch (IOException ex2)
            {
                handler.Error(ex2);
                return ExitCode.IOError;
            }
            catch (NotAdminException ex2)
            {
                handler.Error(ex2);
                return ExitCode.AccessDenied;
            }
        }
        catch (OptionException ex)
        {
            string message = ex.Message + Environment.NewLine + string.Format(Resources.TryHelp, $"{exeName} --help");
            if (!string.IsNullOrEmpty(ex.OptionName)) message = ex.OptionName + ": " + message;
            handler.Error(new OptionException(message, ex.OptionName));
            return ExitCode.InvalidArguments;
        }
        catch (FormatException ex)
        {
            handler.Error(ex);
            return ExitCode.InvalidArguments;
        }
        catch (WebException ex)
        {
            handler.Error(ex);
            return ExitCode.WebError;
        }
        catch (NotSupportedException ex)
        {
            handler.Error(ex);
            return ExitCode.NotSupported;
        }
        catch (PathTooLongException ex) when (
            WindowsUtils.IsWindows10Redstone &&
            RegistryUtils.GetDword(RegKeyFSPolicyUser, RegValueNameLongPaths, defaultValue: RegistryUtils.GetDword(RegKeyFSPolicyMachine, RegValueNameLongPaths)) != 1)
        {
            Debug.Assert(WindowsUtils.IsWindows);
            try
            {
                string message = ex.Message + @" " + Resources.SuggestLongPath;
                if (handler.Ask(message + @" " + Resources.AskTryNow, defaultAnswer: false, alternateMessage: message))
                {
                    RegistryUtils.SetDword(WindowsUtils.IsAdministrator ? RegKeyFSPolicyMachine : RegKeyFSPolicyUser, RegValueNameLongPaths, 1);
                    return (ExitCode)ProcessUtils.Assembly(exeName, args).Run();
                }
                else
                {
                    handler.Error(ex);
                    return ExitCode.IOError;
                }
            }
            catch (OperationCanceledException)
            {
                return ExitCode.UserCanceled;
            }
            catch (PlatformNotSupportedException ex2)
            {
                handler.Error(ex2);
                return ExitCode.NotSupported;
            }
            catch (IOException ex2)
            {
                handler.Error(ex2);
                return ExitCode.IOError;
            }
            catch (NotAdminException ex2)
            {
                handler.Error(ex2);
                return ExitCode.AccessDenied;
            }
        }
        catch (IOException ex)
        {
            handler.Error(ex);
            return ExitCode.IOError;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            handler.Error(ex);
            return ExitCode.AccessDenied;
        }
        catch (InvalidDataException ex)
        {
            handler.Error(ex);
            return ExitCode.InvalidData;
        }
        catch (SignatureException ex)
        {
            handler.Error(ex);
            return ExitCode.InvalidSignature;
        }
        catch (DigestMismatchException ex)
        {
            Log.Info(ex.LongMessage);
            handler.Error(ex);
            return ExitCode.DigestMismatch;
        }
        catch (SolverException ex)
        {
            handler.Error(ex);
            return ExitCode.SolverError;
        }
        catch (ExecutorException ex)
        {
            handler.Error(ex);
            return ExitCode.ExecutorError;
        }
        #endregion

        finally
        {
            handler.CloseUI();
        }
    }

    /// <summary>
    /// Tries to run a command in another instance of Zero Install deployed on this system.
    /// </summary>
    /// <param name="exeName">The name of the executable to call in the target instance.</param>
    /// <param name="args">The arguments to pass to the target instance.</param>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <param name="needsMachineWide"><c>true</c> if a machine-wide install location is required; <c>false</c> if a user-specific location will also do.</param>
    /// <returns>The exit code returned by the other instance; <c>null</c> if no other instance could be found.</returns>
    /// <exception cref="IOException">There was a problem launching the target instance.</exception>
    /// <exception cref="NotAdminException">The target process requires elevation.</exception>
    private static ExitCode? TryRunOtherInstance(string exeName, string[] args, ICommandHandler handler, bool needsMachineWide)
    {
        if (ZeroInstallDeployment.FindOther(needsMachineWide) is {} installLocation)
        {
            Log.Info($"Redirecting to Zero Install instance at: {installLocation}");
            handler.DisableUI();
            return (ExitCode)ProcessUtils.Assembly(Path.Combine(installLocation, exeName), args).Run();
        }
        else return null;
    }

    /// <summary>
    /// Deploys a new instance of Zero Install instance and runs a command in it.
    /// </summary>
    /// <param name="exeName">The name of the executable to call in the target instance.</param>
    /// <param name="args">The arguments to pass to the target instance.</param>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <param name="machineWide"><c>true</c> to deploy to a machine-wide location; <c>false</c> to deploy to a user-specific location.</param>
    /// <returns>The exit code returned by the other instance; <c>null</c> if no other instance could be found.</returns>
    /// <exception cref="IOException">There was a problem launching the target instance.</exception>
    /// <exception cref="NotAdminException">The target process requires elevation.</exception>
    private static ExitCode DeployAndRunOtherInstance(string exeName, string[] args, ICommandHandler handler, bool machineWide)
    {
        Log.Info("Deploying new Zero Install instance to redirect to");

        var deployResult = Run(exeName,
            machineWide
                ? [Self.Name, Self.Deploy.Name, "--library", "--machine"]
                : [Self.Name, Self.Deploy.Name, "--library"],
            handler);
        if (deployResult != ExitCode.OK) return deployResult;

        return TryRunOtherInstance(exeName, [DeployedIndicatorArg, ..args], handler, machineWide)
            ?? throw new IOException("Unable to find newly deployed Zero Install instance.");
    }
}
