// Embedded source template used by StubBuilder class

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

[assembly: AssemblyTitle("[TITLE]")]

static class Program
{
    public static int Main(string[] args)
        => Run(
            fileName: Path.Combine(GetInstallLocation(), "[EXE]"),
            arguments: "[ARGUMENTS] " + string.Join(" ", args.Select(Escape)));

    private static string Escape(string value)
    {
        value = value.Replace("\"", "\\\"");
        if (value.Any(char.IsWhiteSpace)) value = "\"" + value + "\"";
        return value;
    }

    private static string GetInstallLocation()
    {
        try
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Zero Install", "InstallLocation", defaultValue: null)?.ToString()
                ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Zero Install", "InstallLocation", defaultValue: null)?.ToString()
                ?? "";
        }
        catch (Exception ex)
        {
            // Log error but try to continue without location from registry, just relying on PATH
            LogError(ex);
            return "";
        }
    }

    private static int Run(string fileName, string arguments)
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
                // UAC elevation requires ShellExecute
                return RunInner(fileName, arguments, useShellExecute: true);
            }
            catch (Win32Exception innerEx)
            {
                // UAC cancellation should not be logged as an error
                if (innerEx.NativeErrorCode != Win32Cancelled) LogError(innerEx);

                return innerEx.NativeErrorCode;
            }
        }
        catch (Win32Exception ex)
        {
            LogError(ex);
            return ex.NativeErrorCode;
        }
    }

    private static int RunInner(string fileName, string arguments, bool useShellExecute = false)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments) {UseShellExecute = useShellExecute};
        var process = Process.Start(startInfo);
        process?.WaitForExit();
        return process?.ExitCode ?? 0;
    }

    private static void LogError(Exception ex)
    {
        try
        {
            File.AppendAllText(
                path: Path.Combine(Path.GetTempPath(), "[TITLE] 0install Stub Error Log.txt"),
                contents: ex.ToString() + Environment.NewLine);
        }
        catch (Exception)
        {
            // Avoid hiding the original exception
        }
    }
}
