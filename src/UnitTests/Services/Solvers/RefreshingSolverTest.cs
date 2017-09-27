/*
 * Copyright 2010-2017 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using FluentAssertions;
using Moq;
using Xunit;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    public class RefreshingSolverTest : TestWithMocks
    {
        private readonly Requirements _requirements = RequirementsTest.CreateTestRequirements();
        private readonly Selections _selections = SelectionsTest.CreateTestSelections();

        private readonly Mock<IFeedManager> _feedManagerMock;
        private readonly Mock<ISolver> _innerSolverMock;
        private readonly ISolver _solver;

        public RefreshingSolverTest()
        {
            _innerSolverMock = CreateMock<ISolver>();
            _innerSolverMock.Setup(x => x.Solve(_requirements)).Returns(_selections);

            _feedManagerMock = CreateMock<IFeedManager>();

            _solver = new RefreshingSolver(_innerSolverMock.Object, _feedManagerMock.Object);
        }

        [Fact]
        public void ShouldNotRefreshFreshFeeds()
        {
            _feedManagerMock.SetupGet(x => x.ShouldRefresh).Returns(false);

            _solver.Solve(_requirements).Should().Be(_selections);

            _innerSolverMock.Verify(x => x.Solve(_requirements), Times.Exactly(1));
        }

        [Fact]
        public void ShouldRefreshStaleFeeds()
        {
            _feedManagerMock.SetupGet(x => x.ShouldRefresh).Returns(true);
            _feedManagerMock.SetupSet(x => x.Refresh = true).Verifiable();
            _feedManagerMock.SetupSet(x => x.Stale = false).Verifiable();

            _solver.Solve(_requirements).Should().Be(_selections);

            _innerSolverMock.Verify(x => x.Solve(_requirements), Times.Exactly(2));
        }
    }
}
