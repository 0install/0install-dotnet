// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
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
        public void TestRejectsMissingId()
        {
            ShouldThrow(new Implementation {Version = new ImplementationVersion("1")});
        }

        [Fact]
        public void TestRejectsMissingVersion()
        {
            ShouldThrow(new Implementation {ID = "1"});
        }

        private static void ShouldThrow(Implementation implementation)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action builder = () => new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun));
            builder.Should().Throw<InvalidDataException>();
        }

        [Fact]
        public void TestIsSuitable()
        {
            var implementation = ImplementationTest.CreateTestImplementation();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun))
               .IsSuitable.Should().BeTrue();
        }

        [Fact]
        public void TestIsSuitableArchitecture()
        {
            var implementation = ImplementationTest.CreateTestImplementation();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun, implementation.Architecture))
               .IsSuitable.Should().BeTrue();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun, new Architecture(OS.FreeBsd, Cpu.Ppc)))
               .IsSuitable.Should().BeFalse();
        }

        [Fact]
        public void TestIsSuitableVersionMismatch()
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
        public void TestIsSuitableBuggyInsecure()
        {
            var implementation = ImplementationTest.CreateTestImplementation();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences {Implementations = {new ImplementationPreferences {ID = implementation.ID, UserStability = Stability.Buggy}}}, implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun))
               .IsSuitable.Should().BeFalse();
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences {Implementations = {new ImplementationPreferences {ID = implementation.ID, UserStability = Stability.Insecure}}}, implementation, new Requirements(FeedTest.Test1Uri, Command.NameRun))
               .IsSuitable.Should().BeFalse();
        }
    }
}
