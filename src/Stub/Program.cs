// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

// Stub EXE template used by the StubBuilder class in ZeroInstall.DesktopIntegration.
// The target command-line and title are read from a Win32 resource written by StubBuilder into a copy of this EXE.
// Increment StubBuilder.TemplateRevision when making changes here, so existing stubs get replaced.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

static class Program
{
    public static int Main(string[] args)
    {
        StubData data;
        try
        {
            data = StubData.Load();
        }
        catch (Exception ex)
        {
            LogError("0install", ex);
            return 1;
        }

        return Run(
            data,
            fileName: Path.Combine(GetInstallLocation(data), data.Exe),
            arguments: data.Arguments + " " + string.Join(" ", args.Select(Escape)));
    }

    private static string Escape(string value)
    {
        value = value.Replace("\"", "\\\"");
        if (value.Any(char.IsWhiteSpace)) value = "\"" + value + "\"";
        return value;
    }

    private static string GetInstallLocation(StubData data)
    {
        try
        {
            return GetInstallLocation(@"HKEY_CURRENT_USER\SOFTWARE\Zero Install", data.Exe)
                ?? GetInstallLocation(@"HKEY_LOCAL_MACHINE\SOFTWARE\Zero Install", data.Exe)
                ?? "";
        }
        catch (Exception ex)
        {
            // Log error but try to continue without location from registry, just relying on PATH
            LogError(data.Title, ex);
            return "";
        }
    }

    private static string GetInstallLocation(string registryKey, string exe)
    {
        // Skip deployments that no longer exist, e.g., because they were deleted without unregistering
        string path = Registry.GetValue(registryKey, "InstallLocation", defaultValue: null) as string;
        return !string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, exe))
            ? path
            : null;
    }

    private static int Run(StubData data, string fileName, string arguments)
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
                if (innerEx.NativeErrorCode != Win32Cancelled) LogError(data.Title, innerEx);

                return innerEx.NativeErrorCode;
            }
        }
        catch (Win32Exception ex)
        {
            LogError(data.Title, ex);
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

    private static void LogError(string title, Exception ex)
    {
        try
        {
            string fileName = new string(title.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            File.AppendAllText(
                path: Path.Combine(Path.GetTempPath(), fileName + " 0install Stub Error Log.txt"),
                contents: ex.ToString() + Environment.NewLine);
        }
        catch (Exception)
        {
            // Avoid hiding the original exception
        }
    }
}

/// <summary>
/// Data embedded in the stub EXE by StubBuilder as a UTF-16 RCDATA resource.
/// Contains the fields revision, exe, arguments and title, separated by null characters.
/// </summary>
sealed class StubData
{
    public string Exe { get; }
    public string Arguments { get; }
    public string Title { get; }

    private StubData(string exe, string arguments, string title)
    {
        Exe = exe;
        Arguments = arguments;
        Title = title;
    }

    private const string ResourceName = "ZEROINSTALL_STUB";
    private static readonly IntPtr RT_RCDATA = new(10);

    public static StubData Load()
    {
        var resource = FindResource(IntPtr.Zero, ResourceName, RT_RCDATA);
        if (resource == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Stub data not found. This EXE is a template and cannot be run directly.");
        var handle = LoadResource(IntPtr.Zero, resource);
        if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        var pointer = LockResource(handle);
        if (pointer == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());

        var bytes = new byte[SizeofResource(IntPtr.Zero, resource)];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);

        string[] fields = Encoding.Unicode.GetString(bytes).Split('\0');
        if (fields.Length < 4) throw new InvalidDataException("Stub data is incomplete.");
        return new(exe: fields[1], arguments: fields[2], title: fields[3]);
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindResource(IntPtr hModule, string lpName, IntPtr lpType);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32", SetLastError = true)]
    private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);
}
