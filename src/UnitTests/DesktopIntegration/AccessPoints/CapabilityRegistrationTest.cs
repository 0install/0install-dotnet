// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Contains test methods for <see cref="CapabilityRegistration"/>.
/// </summary>
public class CapabilityRegistrationTest
{
    [Fact]
    public void GetConflictIDs()
    {
        var capabilityRegistration = new CapabilityRegistration();
        var appEntry = new AppEntry
        {
            InterfaceUri = FeedTest.Test1Uri,
            Name = "Test",
            CapabilityLists =
            {
                new() {Entries = {new Model.Capabilities.FileType {ID = "test1"}}},
                new() {Entries = {new Model.Capabilities.FileType {ID = "test2"}}}
            }
        };

        capabilityRegistration.GetConflictIDs(appEntry)
                              .Should().Equal("progid:test1", "progid:test2");
    }
}
