/*
 * Copyright 2010-2016 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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

            using (var tempDir = new TemporaryDirectory("0install-unit-test"))
            {
                string path = Path.Combine(tempDir, "shortcut.lnk");
                Shortcut.Create(path, targetPath: "xyz");
                File.Exists(path).Should().BeTrue();
            }
        }
    }
}
