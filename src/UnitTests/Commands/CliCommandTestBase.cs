// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Linq.Expressions;
using FluentAssertions.Execution;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands;

/// <summary>
/// Contains common code for testing specific <see cref="CliCommand"/>s.
/// </summary>
/// <typeparam name="TCommand">The specific type of <see cref="CliCommand"/> to test.</typeparam>
public abstract class CliCommandTestBase<TCommand> : TestWithMocksAndRedirect
    where TCommand : CliCommand
{
    protected readonly MockCommandHandler Handler = new() {Verbosity = Verbosity.Verbose};

    /// <summary>
    /// The command to be tested (system under test).
    /// </summary>
    protected readonly TCommand Sut;

    protected CliCommandTestBase()
    {
        var commandMock = new Mock<TCommand>(Handler) {CallBase = true};

        void SetMock<TProperty>(Expression<Func<TCommand, TProperty>> expression)
            where TProperty : class => commandMock.SetupGet(expression).Returns(GetMock<TProperty>().Object);

        commandMock.Object.ImplementationStore = GetMock<IImplementationStore>().Object;
        commandMock.SetupGet(x => x.Config).Returns(new Config {SelfUpdateUri = null});
        SetMock(x => x.FeedCache);
        SetMock(x => x.CatalogManager);
        SetMock(x => x.OpenPgp);
        SetMock(x => x.PackageManager);
        SetMock(x => x.Solver);
        SetMock(x => x.Fetcher);
        SetMock(x => x.Executor);
        SetMock(x => x.SelectionsManager);

        Sut = commandMock.Object;
    }

    /// <summary>
    /// Verifies that calling <see cref="CliCommand.Parse"/> and <see cref="Execute"/> causes a specific result.
    /// </summary>
    /// <param name="expectedOutput">The expected string for a <see cref="ITaskHandler.Output"/> call; <c>null</c> if none.</param>
    /// <param name="expectedExitCode">The expected exit status code returned by <see cref="Execute"/>.</param>
    /// <param name="args">The arguments to pass to <see cref="CliCommand.Parse"/>.</param>
    protected void RunAndAssert(string? expectedOutput, ExitCode expectedExitCode, params string[] args)
    {
        Sut.Parse(args);
        Sut.Execute().Should().Be(expectedExitCode);
        Handler.LastOutput.Should().Be(expectedOutput);
    }

    /// <summary>
    /// Verifies that calling <see cref="CliCommand.Parse"/> and <see cref="Execute"/> causes a specific result.
    /// </summary>
    /// <param name="expectedOutput">The expected tabular data for a <see cref="ITaskHandler.Output{T}(string,IEnumerable{T})"/> call.</param>
    /// <param name="expectedExitCode">The expected exit status code returned by <see cref="Execute"/>.</param>
    /// <param name="args">The arguments to pass to <see cref="CliCommand.Parse"/>.</param>
    protected void RunAndAssert<T>(IEnumerable<T> expectedOutput, ExitCode expectedExitCode, params string[] args)
    {
        Sut.Parse(args);
        Sut.Execute().Should().Be(expectedExitCode);
        Handler.LastOutputObjects.Should().BeEquivalentTo(expectedOutput);
    }
}
