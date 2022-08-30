// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Contains test methods for <see cref="SelectionCandidateComparer"/>.
/// </summary>
public class SelectionCandidateComparerTest
{
    [Fact]
    public void PreferStability() => TestSort(
        new(Stability.Stable, NetworkLevel.Full, new(), isCached: _ => true),
        better: new() {ID = "stable", Version = new("1"), Stability = Stability.Stable},
        worse: new() {ID = "testing", Version = new("2"), Stability = Stability.Testing});

    [Fact]
    public void IgnoreStabilityDifferencesAboveTargetPolicy() => TestSort(
        new(Stability.Testing, NetworkLevel.Full, new(), isCached: _ => true),
        better: new() {ID = "testing", Version = new("2"), Stability = Stability.Testing},
        worse: new() {ID = "stable", Version = new("1"), Stability = Stability.Stable});

    [Fact]
    public void PreferNativePackagesIgnoringVersionModifiers() => TestSort(
        new(Stability.Testing, NetworkLevel.Full, new(), isCached: _ => true),
        better: new() {ID = "package", Version = new("1-1"), Stability = Stability.Packaged},
        worse: new() {ID = "normal", Version = new("1-2"), Stability = Stability.Stable});

    [Fact]
    public void PreferCachedForEqualVersions() => TestSort(
        new(Stability.Testing, NetworkLevel.Full, new(), isCached: x => x.ID == "cached"),
        better: new() {ID = "cached", Version = new("1")},
        worse: new() {ID = "not-cached", Version = new("1")});

    [Fact]
    public void PreferNewerForFullNetwork() => TestSort(
        new(Stability.Testing, NetworkLevel.Full, new(), isCached: x => x.ID == "cached"),
        better: new() {ID = "not-cached", Version = new("2")},
        worse: new() {ID = "cached", Version = new("1")});

    [Fact]
    public void PreferCachedForLimitedNetwork() => TestSort(
        new(Stability.Testing, NetworkLevel.Minimal, new(), isCached: x => x.ID == "cached"),
        better: new() {ID = "cached", Version = new("1")},
        worse: new() {ID = "not-cached", Version = new("2")});

    [Fact]
    public void PreferSpecificOS() => TestSort(
        new(Stability.Testing, NetworkLevel.Full, new(), isCached: _ => true),
        better: new() {ID = "1a", Version = new("1"), Architecture = new(OS.Linux)},
        worse: new() {ID = "1b", Version = new("1"), Architecture = new(OS.Posix)});

    [Fact]
    public void PreferSpecificCpu() => TestSort(
        new(Stability.Testing, NetworkLevel.Full, new(), isCached: _ => true),
        better: new() {ID = "1a", Version = new("1"), Architecture = new(OS.All, Cpu.I686)},
        worse: new() {ID = "1b", Version = new("1"), Architecture = new(OS.All, Cpu.I486)});

    private static void TestSort(SelectionCandidateComparer comparer, Implementation better, Implementation worse)
    {
        SelectionCandidate ToCandidate(Implementation implementation)
            => new(FeedTest.Test1Uri, new(), implementation, new(FeedTest.Test1Uri, command: ""));

        var a = ToCandidate(better);
        var b = ToCandidate(worse);
        var list = new List<SelectionCandidate> {b, a, a, b};
        list.Sort(comparer);
        list.Should().Equal(a, a, b, b);
    }
}
