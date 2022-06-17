// Embedded source template used by StubBuilder class

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

[assembly: AssemblyTitle("[TITLE]")]

static int RunInner(string fileName, string arguments, bool useShellExecute = false)
{
    var startInfo = new ProcessStartInfo(fileName, arguments) {UseShellExecute = useShellExecute};
    var process = Process.Start(startInfo);
    process?.WaitForExit();
    return process?.ExitCode ?? 0;
}

static int Run(string fileName, string arguments)
{
    const int Win32RequestedOperationRequiresElevation = 740, Win32Cancelled = 1223;

    try
    {
        return RunInner(fileName, arguments);
    }
    catch (Win32Exception ex) when (ex.NativeErrorCode == Win32RequestedOperationRequiresElevation)
    {
        try
        {
            // UAC handling requires ShellExecute
            return RunInner(fileName, arguments, useShellExecute: true);
        }
        // UAC cancellation should not be treated as a crash
        catch (Win32Exception ex2) when (ex2.NativeErrorCode == Win32Cancelled)
        {
            return 100;
        }
    }
}

static string GetInstallLocation()
{
    try
    {
        return Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Zero Install", "InstallLocation",
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Zero Install", "InstallLocation", "")).ToString();
    }
    catch
    {
        return "";
    }
}

static string Escape(string value)
{
    value = value.Replace("\"", "\\\"");
    if (value.Any(char.IsWhiteSpace)) value = "\"" + value + "\"";
    return value;
}

Run(
    Path.Combine(GetInstallLocation(), "[EXE]"),
    "[ARGUMENTS] " + string.Join(" ", args.Select(Escape)));
