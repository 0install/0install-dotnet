// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Common base class for commands that manage an <see cref="AppList"/>.
/// </summary>
public abstract class AppCommand(ICommandHandler handler) : IntegrationCommand(handler)
{
    protected override int AdditionalArgsMin => 1;
    protected override int AdditionalArgsMax => 1;

    /// <summary>
    /// The interface for the application to perform the operation on.
    /// </summary>
    protected FeedUri InterfaceUri = default!;

    /// <summary>
    /// Manages desktop integration operations.
    /// </summary>
    protected CategoryIntegrationManager IntegrationManager { get; private set; } = default!;

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        try
        {
            InterfaceUri = GetCanonicalUri((AdditionalArgs.Count > 1) ? AdditionalArgs[1] : AdditionalArgs[0]);
            Handler.FeedUri = InterfaceUri;

            using (IntegrationManager = new CategoryIntegrationManager(Config, Handler, MachineWide))
                return ExecuteHelper();
        }
        finally
        {
            BackgroundSelfUpdate();
        }
    }

    /// <summary>
    /// Template method that performs the actual operation.
    /// </summary>
    /// <returns>The exit status code to end the process with.</returns>
    protected abstract ExitCode ExecuteHelper();

    /// <summary>
    /// Creates a new alias.
    /// </summary>
    /// <param name="appEntry">The app entry to add the alias to.</param>
    /// <param name="aliasName">The name of the alias to create.</param>
    /// <param name="command">A command within the interface the alias shall point to; can be <c>null</c>.</param>
    protected void CreateAlias(AppEntry appEntry, string aliasName, string? command = null)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (string.IsNullOrEmpty(aliasName)) throw new ArgumentNullException(nameof(aliasName));
        #endregion

        CheckInstallBase();

        // Check this before modifying the environment
        bool needsReopenTerminal = NeedsReopenTerminal(MachineWide);

        // Apply the new alias
        var alias = new AppAlias {Name = aliasName, Command = command};
        IntegrationManager.AddAccessPoints(appEntry, FeedManager[InterfaceUri], [alias]);

        string message = string.Format(Resources.AliasCreated, aliasName, appEntry.Name);
        if (needsReopenTerminal) message += Environment.NewLine + Resources.ReopenTerminal;
        Handler.OutputLow(Resources.DesktopIntegration, message);
    }

    /// <summary>
    /// Determines whether the user may need to reopen the terminal to be able to use newly created aliases.
    /// </summary>
    private static bool NeedsReopenTerminal(bool machineWide)
    {
        // Non-windows terminals may require rehashing to find new aliases
        if (!WindowsUtils.IsWindows) return true;

        string pathVar = Environment.GetEnvironmentVariable("PATH", machineWide ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User) ?? "";
        string stubDirPath = DesktopIntegration.Windows.AppAlias.GetStubDir(machineWide);
        return !pathVar.Split(Path.PathSeparator).Any(x => StringUtils.EqualsIgnoreCase(x, stubDirPath));
    }
}
