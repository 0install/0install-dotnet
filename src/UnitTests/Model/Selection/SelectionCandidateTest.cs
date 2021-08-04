// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;
using ZeroInstall.Model.Preferences;

namespace ZeroInstall.Model.Selection
{
    /// <summary>
    /// Contains test methods for <see cref="SelectionCandidate"/>.
    /// </summary>
    public class SelectionCandidateTest
    {
        [Fact]
        public void IsSuitable()
        {
            var implementation = ImplementationTest.CreateTestImplementation();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun))
               .IsSuitable.Should().BeTrue();
        }

        [Fact]
        public void IsSuitableArchitecture()
        {
            var implementation = ImplementationTest.CreateTestImplementation();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun, implementation.Architecture))
               .IsSuitable.Should().BeTrue();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun, new Architecture(OS.FreeBsd, Cpu.Ppc)))
               .IsSuitable.Should().BeFalse();
        }

        [Fact]
        public void IsSuitableVersionMismatch()
        {
            var implementation = ImplementationTest.CreateTestImplementation();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun)
            {
                ExtraRestrictions = {{FeedTest.Test1Uri, new VersionRange("..!1.1")}}
            }).IsSuitable.Should().BeTrue();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun)
            {
                ExtraRestrictions = {{FeedTest.Test1Uri, new VersionRange("..!1.0")}}
            }).IsSuitable.Should().BeFalse();
        }

        [Fact]
        public void IsSuitableBuggyInsecure()
        {
            var implementation = ImplementationTest.CreateTestImplementation();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences {Implementations = {new ImplementationPreferences {ID = implementation.ID, UserStability = Stability.Buggy}}}, implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun))
               .IsSuitable.Should().BeFalse();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences {Implementations = {new ImplementationPreferences {ID = implementation.ID, UserStability = Stability.Insecure}}}, implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun))
               .IsSuitable.Should().BeFalse();
        }
    }
}
