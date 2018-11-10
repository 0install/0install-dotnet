// Embedded source template used by StubBuilder class

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

[assembly: AssemblyTitle("[TITLE]")]

public static class Stub
{
    public static int Main(string[] args)
    {
        return Run("[EXE]", "[ARGUMENTS] " + JoinArgs(args));
    }

    private static string JoinArgs(string[] args)
    {
        StringBuilder output = new StringBuilder();
        bool first = true;
        for (int i = 0; i < args.Length; i++)
        {
            // No separator before first or after last argument
            if (first) first = false;
            else output.Append(' ');

            output.Append(Escape(args[i]));
        }

        return output.ToString();
    }

    private static string Escape(string value)
    {
        value = value.Replace("\"", "\\\"");
        if (ContainsWhitespace(value)) value = "\"" + value + "\"";
        return value;
    }

    private static bool ContainsWhitespace(string text)
    {
        return text.Contains(" ") || text.Contains("\t") || text.Contains("\n") || text.Contains("\r");
    }

    private static int Run(string fileName, string arguments)
    {
        const int Win32RequestedOperationRequiresElevation = 740, Win32Cancelled = 1223;

        try
        {
            // Avoid using ShellExecute if possible
            return Run(fileName, arguments, false);
        }
        catch (Win32Exception ex)
        {
            if (ex.NativeErrorCode == Win32RequestedOperationRequiresElevation)
            {
                try
                {
                    // UAC handling requires ShellExecute
                    return Run(fileName, arguments, true);
                }
                catch (Win32Exception ex2)
                {
                    // UAC cancellation should not be treated as a crash
                    if (ex2.NativeErrorCode == Win32Cancelled) return 100;
                    else throw;
                }
            }
            else throw;
        }
    }

    private static int Run(string fileName, string arguments, bool useShellExecute)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments);
        startInfo.UseShellExecute = useShellExecute;

        Process process = Process.Start(startInfo);
        process.WaitForExit();
        return process.ExitCode;
    }
}
