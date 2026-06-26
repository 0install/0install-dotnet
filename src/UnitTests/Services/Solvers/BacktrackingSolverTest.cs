// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Runs test methods for <see cref="BacktrackingSolver"/>.
/// </summary>
public class BacktrackingSolverTest : SolverTest
{
    protected override ISolver BuildSolver(ISelectionCandidateProvider candidateProvider)
        => new BacktrackingSolver(candidateProvider);

    [Theory]
    [MemberData(nameof(TestCases), MemberType = typeof(SolverTest))]
    public void TestCase(TestCase testCase)
        => AssertTestCase(testCase);
}
