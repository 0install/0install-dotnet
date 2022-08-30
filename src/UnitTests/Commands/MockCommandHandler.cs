// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands;

/// <summary>
/// A minimalistic <see cref="ICommandHandler"/> that allows you to pre-record answers and retrieve output.
/// </summary>
public class MockCommandHandler : MockTaskHandler, ICommandHandler
{
    /// <summary>
    /// Always returns <c>false</c>.
    /// </summary>
    public bool IsGui => false;

    /// <summary>
    /// Always returns <c>false</c>.
    /// </summary>
    public bool Background { get => false; set {} }

    /// <inheritdoc/>
    public FeedUri? FeedUri { get; set; }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void DisableUI() {}

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void CloseUI() {}

    /// <summary>
    /// Last <see cref="Selections"/> passed to <see cref="ShowSelections"/>.
    /// </summary>
    public Selections? LastSelections { get; private set; }

    /// <summary>
    /// Fakes showing <see cref="Selections"/> to the user.
    /// </summary>
    public void ShowSelections(Selections selections, IFeedManager feedManager) => LastSelections = selections;

    /// <inheritdoc/>
    public void CustomizeSelections(Func<Selections> solveCallback) {}

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void ShowIntegrateApp(IntegrationState state) {}

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void ManageStore(IImplementationStore implementationStore, IFeedCache feedCache) {}
}
