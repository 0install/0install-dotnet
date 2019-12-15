// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Moq;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains test methods for <see cref="StubBuilder"/>.
    /// </summary>
    public sealed class StubBuilderTest
    {
        private readonly Mock<IIconStore> iconStoreMock = new Mock<IIconStore>();

        [SkippableFact]
        public void TestBuildStubGui()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");

            var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
            using var tempFile = new TemporaryFile("0install-unit-tests");
            StubBuilder.BuildRunStub(target, tempFile, iconStoreMock.Object, needsTerminal: false);
        }

        [SkippableFact]
        public void TestBuildStubNeedsTerminal()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");

            var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
            using var tempFile = new TemporaryFile("0install-unit-tests");
            StubBuilder.BuildRunStub(target, tempFile, iconStoreMock.Object, needsTerminal: true);
        }
    }
}
