// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Globalization;
using System.Linq;
using FluentAssertions;
using Xunit;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

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
            var requirements = new Requirements("http://test/feed.xml").GetNormalizedAlternatives().First();
            requirements.Should().Be(new Requirements(new FeedUri("http://test/feed.xml"), Command.NameRun, Architecture.CurrentSystem) {Languages = {CultureInfo.CurrentUICulture}});
        }

        [Fact]
        public void GetNormalizedAlternativesHandlesX86OnX64()
        {
            Skip.IfNot(Architecture.CurrentSystem.Cpu == Cpu.X64, "Can only test on X64 systems");

            var requirements = new Requirements("http://test/feed.xml", Command.NameRun, new Architecture(OS.Linux, Cpu.X64)) {Languages = {"fr"}};
            requirements.GetNormalizedAlternatives().Should().Equal(
                new Requirements("http://test/feed.xml", Command.NameRun, new Architecture(OS.Linux, Cpu.X64)) {Languages = {"fr"}},
                new Requirements("http://test/feed.xml", Command.NameRun, new Architecture(OS.Linux, Cpu.I686)) {Languages = {"fr"}});
        }
    }
}
