// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Native;
using Xunit;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Contains test methods for <see cref="ImplementationStoreUtils"/>.
    /// </summary>
    public class ImplementationStoreUtilsTest
    {
        [Fact]
        public void TestDetectImplementationPath()
        {
            ImplementationStoreUtils.DetectImplementationPath(WindowsUtils.IsWindows ? @"C:\some\dir" : "/some/dir")
                      .Should().BeNull();
            ImplementationStoreUtils.DetectImplementationPath(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123")
                      .Should().Be(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123");
            ImplementationStoreUtils.DetectImplementationPath(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123\subdir" : "/some/dir/sha1new=123/subdir")
                      .Should().Be(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123");
        }
    }
}
