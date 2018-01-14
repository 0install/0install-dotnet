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

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Contains test methods for <see cref="IconStore"/>.
    /// </summary>
    public class IconStoreTest : IDisposable
    {
        private readonly TemporaryDirectory _tempDir = new TemporaryDirectory("0install-unit-tests");
        private readonly IconStore _store;

        public IconStoreTest() => _store = new IconStore(new SilentTaskHandler(), _tempDir);

        public void Dispose() => _tempDir.Dispose();

        [Fact]
        public void ShouldEnsureCorrectFileExtension()
        {
            string path = _store.BuildPath(PngIcon(new Uri("http://host/file")), machineWide: false);
            Path.GetExtension(path).Should().Be(".png");
        }

        [Fact]
        public void ShouldReturnCached()
        {
            var icon = PngIcon(new Uri("http://0install.de/feeds/images/test1.png"));
            Inject(icon, iconData: "icon");
            Verify(icon, iconData: "icon");
        }

        [Fact]
        public void ShouldDownloadMissing()
        {
            using (var server = new MicroServer("icon.png", "data".ToStream()))
            {
                var icon = PngIcon(server.FileUri);
                Verify(icon, "data");
            }
        }

        [Fact]
        public void ShouldRefreshStale()
        {
            using (var server = new MicroServer("icon.png", "new".ToStream()))
            {
                var icon = PngIcon(server.FileUri);
                Inject(icon, "old", timestamp: new DateTime(1980, 1, 1));
                Verify(icon, "new");
            }
        }

        [Fact]
        public void ShouldReturnStaleOnRefreshFailure()
        {
            using (var server = new MicroServer("_", new MemoryStream()))
            {
                var icon = PngIcon(new Uri(server.FileUri + "-invalid"));
                Inject(icon, "data", timestamp: new DateTime(1980, 1, 1));
                Verify(icon, "data");
            }
        }

        private static Icon PngIcon(Uri href) => new Icon {Href = href, MimeType = Icon.MimeTypePng};

        private void Inject(Icon icon, string iconData, DateTime? timestamp = null)
        {
            string path = _store.BuildPath(icon, machineWide: false);
            File.WriteAllText(path, iconData);
            if (timestamp.HasValue) File.SetLastWriteTimeUtc(path, timestamp.Value);
        }

        private void Verify(Icon icon, string iconData)
            => File.ReadAllText(_store.GetPath(icon)).Should().Be(iconData);
    }
}
