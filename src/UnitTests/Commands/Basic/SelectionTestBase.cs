// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NDesk.Options;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Feeds;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for commands derived from <see cref="Selection"/>.
/// </summary>
/// <typeparam name="TCommand">The specific type of <see cref="CliCommand"/> to test.</typeparam>
public abstract class SelectionTestBase<TCommand> : CliCommandTestBase<TCommand>
    where TCommand : Selection
{
    [Fact]
    public virtual void ShouldRejectTooManyArgs()
        => Assert.Throws<OptionException>(() => Sut.Parse(["http://example.com/test1.xml", "arg1"]));

    /// <summary>
    /// Configures the <see cref="ISolver"/> mock to expect a call with <see cref="CreateTestRequirements"/>.
    /// </summary>
    /// <returns>The selections returned by the mock; <see cref="Fake.Selections"/>.</returns>
    protected Selections ExpectSolve()
    {
        var requirements = CreateTestRequirements();
        var selections = Fake.Selections;

        GetMock<ISolver>().Setup(x => x.Solve(requirements)).Returns(selections);

        var feed = Fake.Feed;
        GetMock<IFeedCache>().Setup(x => x.GetFeed(Fake.Feed1Uri)).Returns(feed);

        selections.Name = feed.Name;
        return selections;
    }

    /// <summary>
    /// Configures the <see cref="ISelectionsManager"/> mock to expect the listing of uncached implementations.
    /// </summary>
    /// <param name="selections">The selections to check for uncached implementations.</param>
    /// <param name="implementations">The implementations to treat as uncached.</param>
    protected void ExpectListUncached(Selections selections, params Implementation[] implementations)
    {
        GetMock<ISelectionsManager>().Setup(x => x.GetUncached(selections.Implementations)).Returns(selections.Implementations);
        GetMock<ISelectionsManager>().Setup(x => x.GetImplementations(selections.Implementations)).Returns(implementations);
    }

    /// <summary>
    /// Configures the <see cref="ISelectionsManager"/> and <see cref="IFetcher"/> mocks to expect the download of uncached implementations.
    /// </summary>
    /// <param name="selections">The selections to check for uncached implementations.</param>
    /// <param name="implementations">The implementations to treat as uncached.</param>
    protected void ExpectFetchUncached(Selections selections, params Implementation[] implementations)
    {
        ExpectListUncached(selections, implementations);
        var mock = GetMock<IFetcher>();
        foreach (var implementation in implementations)
            mock.Setup(x => x.Fetch(implementation));
    }

    /// <summary>
    /// Verifies that calling <see cref="CliCommand.Parse"/> and <see cref="CliCommand.Execute"/> causes a specific result.
    /// </summary>
    /// <param name="expectedOutput">The expected string for a <see cref="ITaskHandler.Output"/> call; <c>null</c> if none.</param>
    /// <param name="expectedExitCode">The expected exit status code returned by <see cref="CliCommand.Execute"/>.</param>
    /// <param name="expectedSelections">The expected value passed to <see cref="ICommandHandler.ShowSelections"/>.</param>
    /// <param name="args">The arguments to pass to <see cref="CliCommand.Parse"/>.</param>
    protected void RunAndAssert(string? expectedOutput, ExitCode expectedExitCode, Selections? expectedSelections, params string[] args)
    {
        RunAndAssert(expectedOutput, expectedExitCode, args);

        var selections = Handler.LastSelections;
        if (expectedSelections == null) selections.Should().BeNull();
        else
        {
            selections!.InterfaceUri.Should().Be(expectedSelections.InterfaceUri);
            selections.Command.Should().Be(expectedSelections.Command);
            selections.Implementations.Should().Equal(expectedSelections.Implementations);
        }
    }

    protected static Requirements CreateTestRequirements() => new(Fake.Feed1Uri, "command", new Architecture(OS.Windows, Cpu.I586))
    {
        ExtraRestrictions =
        {
            {Fake.Feed1Uri, new VersionRange("1.0..!2.0")},
            {Fake.Feed2Uri, new VersionRange("2.0..!3.0")}
        }
    };
}
