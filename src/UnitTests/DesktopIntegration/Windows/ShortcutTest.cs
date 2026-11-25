// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains test methods for <see cref="Shortcut"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public class ShortcutTest
{
    public ShortcutTest()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Shortcut creation is only available on Windows");
    }

    [Fact]
    public void TestCreate()
    {
        using var tempDir = new TemporaryDirectory("0install-unit-test");
        string path = Path.Combine(tempDir, "shortcut.lnk");
        Shortcut.Create(path, targetPath: "abc", arguments: "xyz");

        var shortcut = ShellLink.Shortcut.ReadFromFile(path);
        shortcut.ExtraData.EnvironmentVariableDataBlock.TargetUnicode.Should().Be("abc");
        shortcut.StringData.CommandLineArguments.Should().Be("xyz");
    }

    [Fact]
    public void RemoveEmptyCategoryDirsNested()
    {
        using var menuDir = new TemporaryDirectory("0install-unit-test");
        Directory.CreateDirectory(Path.Combine(menuDir, "a", "b", "c"));

        Shortcut.RemoveEmptyCategoryDirs(menuDir, "a/b/c");

        Directory.Exists(Path.Combine(menuDir, "a")).Should().BeFalse();
        Directory.Exists(menuDir).Should().BeTrue();
    }

    [Fact]
    public void RemoveEmptyCategoryDirsStopsAtNonEmpty()
    {
        using var menuDir = new TemporaryDirectory("0install-unit-test");
        Directory.CreateDirectory(Path.Combine(menuDir, "a", "b", "c"));
        File.WriteAllText(Path.Combine(menuDir, "a", "other.lnk"), "");

        Shortcut.RemoveEmptyCategoryDirs(menuDir, @"a\b\c");

        Directory.Exists(Path.Combine(menuDir, "a", "b")).Should().BeFalse();
        Directory.Exists(Path.Combine(menuDir, "a")).Should().BeTrue();
    }

    [Fact]
    public void RemoveEmptyCategoryDirsStopsAtMissing()
    {
        using var menuDir = new TemporaryDirectory("0install-unit-test");
        Directory.CreateDirectory(Path.Combine(menuDir, "a"));

        Shortcut.RemoveEmptyCategoryDirs(menuDir, "a/b");

        Directory.Exists(Path.Combine(menuDir, "a")).Should().BeTrue();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("./.")]
    [InlineData("a/.")]
    [InlineData("a//")]
    public void RemoveEmptyCategoryDirsNeverDeletesMenuDir(string category)
    {
        using var menuDir = new TemporaryDirectory("0install-unit-test");
        Directory.CreateDirectory(Path.Combine(menuDir, "a"));

        Shortcut.RemoveEmptyCategoryDirs(menuDir, category);

        Directory.Exists(menuDir).Should().BeTrue();
    }
}
