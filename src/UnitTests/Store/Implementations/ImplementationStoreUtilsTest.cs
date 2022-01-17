// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Contains test methods for <see cref="ImplementationStoreUtils"/>.
/// </summary>
public class ImplementationStoreUtilsTest
{
    [Fact]
    public void NotImplementationPath()
    {
        ImplementationStoreUtils.IsImplementation(WindowsUtils.IsWindows ? @"C:\some\dir" : "/some/dir", out _)
                                .Should().BeFalse();
    }

    [Fact]
    public void ImplementationPath()
    {
        ImplementationStoreUtils.IsImplementation(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123", out string? path)
                                .Should().BeTrue();
        path.Should().Be(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123");
    }

    [Fact]
    public void ImplementationSubDirPath()
    {
        ImplementationStoreUtils.IsImplementation(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123\subdir" : "/some/dir/sha1new=123/subdir", out string? path)
                                .Should().BeTrue();
        path.Should().Be(WindowsUtils.IsWindows ? @"C:\some\dir\sha1new=123" : "/some/dir/sha1new=123");
    }
}
