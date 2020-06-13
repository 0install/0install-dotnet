// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Globalization;
using FluentAssertions;
using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Contains test methods for <see cref="SolverUtils"/>.
    /// </summary>
    public class SolverUtilsTest
    {
        [Fact]
        public void GetNormalizedAlternativesFillsInDefaultValues()
        {
            var requirements = new Requirements("http://test/feed.xml").ForCurrentSystem();
            requirements.Should().Be(new Requirements(new FeedUri("http://test/feed.xml"), Command.NameRun, Architecture.CurrentSystem) {Languages = {CultureInfo.CurrentUICulture}});
        }
    }
}
