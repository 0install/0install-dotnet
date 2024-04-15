// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands;

/// <summary>
/// Uses the stdin/stderr streams to allow users to interact with <see cref="CliCommand"/>s.
/// </summary>
[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Diamond inheritance structure leads to false positive")]
[MustDisposeResource]
public sealed class CliCommandHandler : AnsiCliTaskHandler, ICommandHandler
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

    /// <inheritdoc/>
    public void DisableUI()
    {
        // Console UI only, so nothing to do
    }

    /// <inheritdoc/>
    public void CloseUI()
    {
        // Console UI only, so nothing to do
    }

    /// <inheritdoc/>
    public void ShowSelections(Selections selections, IFeedManager feedManager)
    {}

    /// <inheritdoc/>
    public void CustomizeSelections(Func<Selections> solveCallback) => throw new NeedsGuiException(Resources.NoCustomizeSelectionsInCli);

    /// <inheritdoc/>
    public void ShowIntegrateApp(IntegrationState state) => throw new NeedsGuiException(Resources.IntegrateAppUseGui);
}
