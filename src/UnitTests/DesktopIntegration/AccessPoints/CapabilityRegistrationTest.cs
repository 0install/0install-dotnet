// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Contains test methods for <see cref="CapabilityRegistration"/>.
    /// </summary>
    public class CapabilityRegistrationTest
    {
        [Fact]
        public void TestGetConflictIDs()
        {
            var capabilityRegistration = new CapabilityRegistration();
            var appEntry = new AppEntry
            {
                CapabilityLists =
                {
                    new CapabilityList {Entries = {new Store.Model.Capabilities.FileType {ID = "test1"}}},
                    new CapabilityList {Entries = {new Store.Model.Capabilities.FileType {ID = "test2"}}}
                }
            };

            capabilityRegistration.GetConflictIDs(appEntry)
                                  .Should().Equal("progid:test1", "progid:test2");
        }
    }
}
