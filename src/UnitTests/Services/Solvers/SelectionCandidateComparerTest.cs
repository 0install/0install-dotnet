/*
 * Copyright 2010-2018 Bastian Eicher
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

using System.Collections.Generic;
using FluentAssertions;
using NanoByte.Common.Collections;
using Xunit;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Preferences;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Contains test methods for <see cref="SelectionCandidateComparer"/>.
    /// </summary>
    public class SelectionCandidateComparerTest
    {
        [Fact]
        public void PreferStability() => TestSort(
            new SelectionCandidateComparer(Stability.Stable, NetworkLevel.Full, new LanguageSet(), isCached: _ => true),
            better: new Implementation {ID = "1", Version = new ImplementationVersion("1"), Stability = Stability.Stable},
            worse: new Implementation {ID = "2", Version = new ImplementationVersion("2"), Stability = Stability.Testing});

        [Fact]
        public void IgnoreStabilityAbovePolicy() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), isCached: _ => true),
            better: new Implementation {ID = "2", Version = new ImplementationVersion("2"), Stability = Stability.Testing},
            worse: new Implementation {ID = "1", Version = new ImplementationVersion("1"), Stability = Stability.Stable});

        [Fact]
        public void PreferCached() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), isCached: x => x.ID == "1a"),
            better: new Implementation {ID = "1a", Version = new ImplementationVersion("1")},
            worse: new Implementation {ID = "1b", Version = new ImplementationVersion("1")});

        [Fact]
        public void PreferCachedForLimitedNetwork() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Minimal, new LanguageSet(), isCached: x => x.ID == "1"),
            better: new Implementation {ID = "1", Version = new ImplementationVersion("1")},
            worse: new Implementation {ID = "2", Version = new ImplementationVersion("2")});

        [Fact]
        public void PreferSpecificOS() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), isCached: _ => true),
            better: new Implementation {ID = "1a", Version = new ImplementationVersion("1"), Architecture = new Architecture(OS.Linux, Cpu.All)},
            worse: new Implementation {ID = "1b", Version = new ImplementationVersion("1"), Architecture = new Architecture(OS.Posix, Cpu.All)});

        [Fact]
        public void PreferSpecificCpu() => TestSort(
            new SelectionCandidateComparer(Stability.Testing, NetworkLevel.Full, new LanguageSet(), isCached: _ => true),
            better: new Implementation {ID = "1a", Version = new ImplementationVersion("1"), Architecture = new Architecture(OS.All, Cpu.I686)},
            worse: new Implementation {ID = "1b", Version = new ImplementationVersion("1"), Architecture = new Architecture(OS.All, Cpu.I486)});

        private static void TestSort(SelectionCandidateComparer comparer, Implementation better, Implementation worse)
        {
            SelectionCandidate ToCandidate(Implementation implementation)
                => new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), implementation, new Requirements {InterfaceUri = FeedTest.Test1Uri, Command = ""});

            var a = ToCandidate(better);
            var b = ToCandidate(worse);
            var list = new List<SelectionCandidate> {b, a, a, b};
            list.Sort(comparer);
            list.Should().Equal(a, a, b, b);
        }
    }
}
