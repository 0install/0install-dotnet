// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="FeedElement"/>.
/// </summary>
public class FeedElementTest
{
    [Fact]
    public void FilterMismatch()
    {
        FeedElement.FilterMismatch(new EntryPoint {Command = Command.NameRun}).Should().BeFalse();
        FeedElement.FilterMismatch(new EntryPoint {Command = Command.NameRun, IfZeroInstallVersion = new VersionRange("0..")}).Should().BeFalse();
        FeedElement.FilterMismatch(new EntryPoint {Command = Command.NameRun, IfZeroInstallVersion = new VersionRange("..!0")}).Should().BeTrue();
    }
}
