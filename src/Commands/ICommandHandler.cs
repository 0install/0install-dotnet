// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands;

/// <summary>
/// Callback methods to allow users to interact with <see cref="CliCommand"/>s.
/// </summary>
/// <remarks>The methods may be called from a background thread. Implementations apply appropriate thread-synchronization to update UI elements.</remarks>
public interface ICommandHandler : ITaskHandler
{
    /// <summary>
    /// Indicates whether this handler is a GUI.
    /// </summary>
    bool IsGui { get; }

    /// <summary>
    /// Hides the GUI and uses something like a tray icon instead. Has no effect when <see cref="IsGui"/> is <c>false</c>.
    /// </summary>
    bool Background { get; set; }

    /// <summary>
    /// The URI of the Zero Install feed the current operation relates to.
    /// </summary>
    /// <remarks>This can be used to apply application-specific visual branding.</remarks>
    FeedUri? FeedUri { get; set; }

    /// <summary>
    /// Disables any persistent UI elements that were created but still leaves them visible.
    /// </summary>
    void DisableUI();

    /// <summary>
    /// Closes any persistent UI elements that were created.
    /// </summary>
    void CloseUI();

    /// <summary>
    /// Shows the <see cref="Selections"/> made by the solver to the user.
    /// Returns immediately. May be ignored by some implementations.
    /// </summary>
    /// <param name="selections">The <see cref="Selections"/> as provided by the solver.</param>
    /// <param name="feedManager">The feed manager used to retrieve feeds for additional information about implementations.</param>
    void ShowSelections(Selections selections, IFeedManager feedManager);

    /// <summary>
    /// Allows the user to customize the interface preferences and rerun the solver if desired.
    /// Returns once the user is finished.
    /// </summary>
    /// <param name="solveCallback">Called after interface preferences have been changed and the solver needs to be rerun.</param>
    void CustomizeSelections(Func<Selections> solveCallback);

    /// <summary>
    /// Displays application integration options to the user.
    /// Returns once the user is finished.
    /// </summary>
    /// <param name="state">A View-Model for modifying the current desktop integration state.</param>
    /// <exception cref="OperationCanceledException">The user does not want any changes to be applied.</exception>
    /// <remarks>The caller is responsible for applying changes.</remarks>
    void ShowIntegrateApp(IntegrationState state);
}
