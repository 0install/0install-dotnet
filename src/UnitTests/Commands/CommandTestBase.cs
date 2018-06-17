// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using FluentAssertions.Execution;
using JetBrains.Annotations;
using Moq;
using NanoByte.Common.Tasks;
using ZeroInstall.Services;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.PackageManagers;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

#if !NETCOREAPP2_0
using ZeroInstall.Commands.Desktop.Utils;
#endif

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Contains common code for testing specific <see cref="CommandBase"/>s.
    /// </summary>
    /// <typeparam name="TCommand">The specific type of <see cref="CommandBase"/> to test.</typeparam>
    public abstract class CommandTestBase<TCommand> : TestWithMocksAndRedirect
        where TCommand : CommandBase
    {
        protected readonly MockCommandHandler Handler = new MockCommandHandler();

        /// <summary>
        /// The object to be tested (system under test).
        /// </summary>
        protected readonly TCommand Sut;

        private readonly IDictionary<Type, Mock> _mocks = new Dictionary<Type, Mock>();

        /// <summary>
        /// Retrieves a <see cref="Mock"/> for a specific type. Multiple requests for the same type return the same mock instance.
        /// These are the same mocks that are injected into the <see cref="Sut"/>.
        /// </summary>
        /// <remarks>All created <see cref="Mock"/>s are automatically verified after the test completes.</remarks>
        protected Mock<T> GetMock<T>() where T : class => (Mock<T>)_mocks[typeof(T)];

        protected CommandTestBase()
        {
            Sut = (TCommand)(typeof(TCommand).GetConstructor(new[] {typeof(ICommandHandler)}).Invoke(new object[] {Handler}));

            T BuildMock<T>() where T : class
            {
                var mock = MockRepository.Create<T>();
                _mocks[typeof(T)] = mock;
                return mock.Object;
            }

            Sut.Config = new Config();
            Sut.FeedCache = BuildMock<IFeedCache>();
            Sut.CatalogManager = BuildMock<ICatalogManager>();
            Sut.OpenPgp = BuildMock<IOpenPgp>();
            Sut.TrustDB = new TrustDB();
            Sut.Store = BuildMock<IStore>();
            Sut.PackageManager = BuildMock<IPackageManager>();
            Sut.Solver = BuildMock<ISolver>();
            Sut.Fetcher = BuildMock<IFetcher>();
            Sut.Executor = BuildMock<IExecutor>();
            Sut.SelectionsManager = BuildMock<ISelectionsManager>();

#if !NETCOREAPP2_0
            SelfUpdateUtils.NoAutoCheck = true;
#endif
        }

        /// <summary>
        /// Verifies that calling <see cref="CommandBase.Parse"/> and <see cref="Execute"/> causes a specific reuslt.
        /// </summary>
        /// <param name="expectedOutput">The expected string for a <see cref="ITaskHandler.Output"/> call; <c>null</c> if none.</param>
        /// <param name="expectedExitCode">The expected exit status code returned by <see cref="Execute"/>.</param>
        /// <param name="args">The arguments to pass to <see cref="CommandBase.Parse"/>.</param>
        protected void RunAndAssert([CanBeNull] string expectedOutput, ExitCode expectedExitCode, params string[] args)
        {
            Sut.Parse(args);
            Sut.Execute().Should().Be(expectedExitCode);
            Handler.LastOutput.Should().Be(expectedOutput);
        }

        /// <summary>
        /// Verifies that calling <see cref="CommandBase.Parse"/> and <see cref="Execute"/> causes a specific reuslt.
        /// </summary>
        /// <param name="expectedOutput">The expected tabular data for a <see cref="ITaskHandler.Output{T}"/> call.</param>
        /// <param name="expectedExitCode">The expected exit status code returned by <see cref="Execute"/>.</param>
        /// <param name="args">The arguments to pass to <see cref="CommandBase.Parse"/>.</param>
        protected void RunAndAssert<T>(IEnumerable<T> expectedOutput, ExitCode expectedExitCode, params string[] args)
        {
            Sut.Parse(args);
            Sut.Execute().Should().Be(expectedExitCode);
            Handler.LastOutputObjects.Should().Equal(expectedOutput);
        }
    }
}