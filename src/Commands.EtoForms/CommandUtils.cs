// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Reflection;

namespace ZeroInstall.Commands.EtoForms;

/// <summary>
/// Helpers for running GUI commands in the executable hosting this library.
/// </summary>
public static class CommandUtils
{
    /// <summary>
    /// Starts a GUI command and does not wait for it to complete.
    /// </summary>
    /// <param name="args">Command name with arguments to execute.</param>
    public static void Start(params string?[] args)
    {
        try
        {
            GetStartInfo(args).Start();
        }
        #region Error handling
        catch (IOException ex)
        {
            Msg.Inform(null, ex.Message, MsgSeverity.Error);
        }
        #endregion
    }

    /// <summary>
    /// Starts a GUI command elevated as Administrator and does not wait for it to complete.
    /// </summary>
    /// <param name="args">Command name with arguments to execute.</param>
    public static void StartAsAdmin(params string?[] args)
    {
        try
        {
            GetStartInfo(args).AsAdmin().Start();
        }
        #region Error handling
        catch (OperationCanceledException) {}
        catch (Exception ex) when (ex is PlatformNotSupportedException or IOException)
        {
            Msg.Inform(null, ex.Message, MsgSeverity.Error);
        }
        #endregion
    }

    /// <summary>
    /// Starts the executable hosting this library (e.g., <c>0install-win</c>) with the specified arguments.
    /// </summary>
    private static ProcessStartInfo GetStartInfo(string?[] args)
        => ProcessUtils.Assembly(
            Assembly.GetEntryAssembly()?.GetName().Name ?? throw new InvalidOperationException("Unable to determine entry assembly."),
            args.WhereNotNull().ToArray());

    /// <summary>
    /// Opens a URL or a file system path using the default application for it. Informs the user about any errors.
    /// </summary>
    /// <param name="owner">The parent window for error messages; can be <c>null</c>.</param>
    /// <param name="target">The URL or path to open.</param>
    public static void Open(Control? owner, string target)
    {
        try
        {
            Application.Instance.Open(target);
        }
        #region Error handling
        catch (Exception ex)
        {
            Msg.Inform(owner, ex.Message, MsgSeverity.Error);
        }
        #endregion
    }
}
