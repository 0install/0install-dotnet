// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Native;
using Xunit;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Contains test methods for <see cref="StoreUtils"/>.
    /// </summary>
    public class StoreUtilsTest
    {
        [Fact]
        public void TestDetectImplementationPath()
        {
            StoreUtils.DetectImplementationPath(WindowsUtils.IsWindows ? @"C:\some\dir" : "/some/dir")
                      .Should().BeNull();
            StoreUtils.DetectImplementationPath(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123")
                      .Should().Be(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123");
            StoreUtils.DetectImplementationPath(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123\subdir" : "/some/dir/sha1new=123/subdir")
                      .Should().Be(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123");
        }
    }
}
