// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NanoByte.Common.Tasks;
using ZeroInstall.Store;

namespace ZeroInstall.Commands
{
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

        private readonly Dictionary<Type, Mock> _mocks = new();

        /// <summary>
        /// Retrieves a <see cref="Mock"/> for a specific type. Multiple requests for the same type return the same mock instance.
        /// These are the same mocks that are injected into the <see cref="Sut"/>.
        /// </summary>
        /// <remarks>All created <see cref="Mock"/>s are automatically verified after the test completes.</remarks>
        protected Mock<T> GetMock<T>() where T : class => (Mock<T>)_mocks[typeof(T)];

        protected CliCommandTestBase()
        {
            var commandMock = new Mock<TCommand>(Handler) {CallBase = true};

            void SetupGet<TProperty>(Expression<Func<TCommand, TProperty>> expression, TProperty? value = null)
                where TProperty : class
            {
                if (value == null)
                {
                    var mock = MockRepository.Create<TProperty>();
                    _mocks[typeof(TProperty)] = mock;
                    value = mock.Object;
                }
                commandMock.SetupGet(expression).Returns(value);
            }

            SetupGet(x => x.Config, new Config {SelfUpdateUri = null});
            SetupGet(x => x.FeedCache);
            SetupGet(x => x.CatalogManager);
            SetupGet(x => x.OpenPgp);
            SetupGet(x => x.ImplementationStore);
            SetupGet(x => x.PackageManager);
            SetupGet(x => x.Solver);
            SetupGet(x => x.Fetcher);
            SetupGet(x => x.Executor);
            SetupGet(x => x.SelectionsManager);

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
}
