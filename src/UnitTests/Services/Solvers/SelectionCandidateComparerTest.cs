// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using FluentAssertions;
using NanoByte.Common.Collections;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Contains test methods for <see cref="SelectionCandidateComparer"/>.
    /// </summary>
    public class SelectionCandidateComparerTest
    {
        [Fact]
        public void PreferStability() => TestSort(
            new SelectionCandidateComparer(Stability.Stable, NetworkLevel.Full, new LanguageSet(), IsCached: _ => true),
            better: new Implementation {ID = "1", Version = new("1"), Stability = Stability.Stable},
            worse: new Implementation {ID = "2", Version = new("2"), Stability = Stability.Testing});

        [Fact]
        public void IgnoreStabilityAbovePolicy() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), IsCached: _ => true),
            better: new Implementation {ID = "2", Version = new("2"), Stability = Stability.Testing},
            worse: new Implementation {ID = "1", Version = new("1"), Stability = Stability.Stable});

        [Fact]
        public void PreferNativePackagesIgnoringVersionModifiers() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), IsCached: _ => true),
            better: new Implementation {ID = "1-1", Version = new("2"), Stability = Stability.Packaged},
            worse: new Implementation {ID = "1-2", Version = new("1"), Stability = Stability.Stable});

        [Fact]
        public void PreferCached() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), IsCached: x => x.ID == "1a"),
            better: new Implementation {ID = "1a", Version = new("1")},
            worse: new Implementation {ID = "1b", Version = new("1")});

        [Fact]
        public void PreferCachedForLimitedNetwork() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Minimal, new LanguageSet(), IsCached: x => x.ID == "1"),
            better: new Implementation {ID = "1", Version = new("1")},
            worse: new Implementation {ID = "2", Version = new("2")});

        [Fact]
        public void PreferSpecificOS() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), IsCached: _ => true),
            better: new Implementation {ID = "1a", Version = new("1"), Architecture = new(OS.Linux, Cpu.All)},
            worse: new Implementation {ID = "1b", Version = new("1"), Architecture = new(OS.Posix, Cpu.All)});

        [Fact]
        public void PreferSpecificCpu() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), IsCached: _ => true),
            better: new Implementation {ID = "1a", Version = new("1"), Architecture = new(OS.All, Cpu.I686)},
            worse: new Implementation {ID = "1b", Version = new("1"), Architecture = new(OS.All, Cpu.I486)});

        private static void TestSort(SelectionCandidateComparer comparer, Implementation better, Implementation worse)
        {
            SelectionCandidate ToCandidate(Implementation implementation)
                => new(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements(FeedTest.Test1Uri, command: ""));

            var a = ToCandidate(better);
            var b = ToCandidate(worse);
            var list = new List<SelectionCandidate> {b, a, a, b};
            list.Sort(comparer);
            list.Should().Equal(a, a, b, b);
        }
    }
}
