// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Tasks;
using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Callback methods to allow users to interact with <see cref="CliCommand"/>s.
    /// </summary>
    /// <remarks>The methods may be called from a background thread. Implementations apply appropriate thread-synchronization to update UI elements.</remarks>
    public interface ICommandHandler : ITaskHandler
    {
        /// <summary>
        /// Hides the GUI and uses something like a tray icon instead. Has no effect in CLI mode.
        /// </summary>
        bool Background { get; set; }

        /// <summary>
        /// Disables any persistent UI elements that were created but still leaves them visible.
        /// </summary>
        void DisableUI();

        /// <summary>
        /// Closes any persistent UI elements that were created.
        /// </summary>
        void CloseUI();

        /// <summary>
        /// Shows the user the <see cref="Selections"/> made by the solver.
        /// Returns immediately. Will be ignored by non-GUI interfaces.
        /// </summary>
        /// <param name="selections">The <see cref="Selections"/> as provided by the solver.</param>
        /// <param name="feedManager">The feed manager used to retrieve feeds for additional information about implementations.</param>
        void ShowSelections(Selections selections, IFeedManager feedManager);

        /// <summary>
        /// Allows the user to customize the interface preferences and rerun the solver if desired.
        /// Returns once the user is satisfied with her choice. Will be ignored by non-GUI interfaces.
        /// </summary>
        /// <param name="solveCallback">Called after interface preferences have been changed and the solver needs to be rerun.</param>
        void CustomizeSelections(Func<Selections> solveCallback);

        /// <summary>
        /// Displays application integration options to the user.
        /// </summary>
        /// <param name="state">A View-Model for modifying the current desktop integration state.</param>
        /// <exception cref="OperationCanceledException">The user does not want any changes to be applied.</exception>
        /// <remarks>The caller is responsible for saving any changes.</remarks>
        void ShowIntegrateApp(IntegrationState state);

        /// <summary>
        /// Displays a user interface for managing <see cref="IImplementationStore"/>s.
        /// </summary>
        /// <param name="implementationStore">The <see cref="IImplementationStore"/> to manage.</param>
        /// <param name="feedCache">Information about implementations found in the <paramref name="implementationStore"/> are extracted from here.</param>
        void ManageStore(IImplementationStore implementationStore, IFeedCache feedCache);
    }
}
