// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Moq;
using NanoByte.Common.Tasks;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Runs test methods for <see cref="BacktrackingSolver"/>.
    /// </summary>
    public class BacktrackingSolverTest : SolverTest
    {
        protected override ISolver BuildSolver(IFeedManager feedManager)
            => new BacktrackingSolver(
                new SelectionCandidateProvider(new Config(), feedManager, new Mock<IImplementationStore>(MockBehavior.Loose).Object, new StubPackageManager()),
                new SilentTaskHandler());
    }
}
