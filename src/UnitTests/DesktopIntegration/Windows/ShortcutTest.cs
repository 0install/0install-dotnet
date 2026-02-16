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
}
