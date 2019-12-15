// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains test methods for <see cref="Shortcut"/>.
    /// </summary>
    public class ShortcutTest
    {
        [SkippableFact]
        public void TestCreate()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "Shortcut files (.lnk) are only used on Windows");

            using var tempDir = new TemporaryDirectory("0install-unit-test");
            string path = Path.Combine(tempDir, "shortcut.lnk");
            Shortcut.Create(path, targetPath: "xyz");
            File.Exists(path).Should().BeTrue();
        }
    }
}
