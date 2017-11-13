/*
 * Copyright 2010-2017 Bastian Eicher
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

using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Contains test methods for <see cref="ManifestUtils"/>.
    /// </summary>
    public class ManifestUtilsTest
    {
        [Fact]
        public void TestCalculateDigest()
        {
            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                string digest = ManifestUtils.CalculateDigest(testDir, ManifestFormat.Sha256New, new SilentTaskHandler());
                digest.Should().StartWith("sha256new_");
            }
        }

        [Fact]
        public void TestGenerateDigest()
        {
            using (var testDir = new TemporaryDirectory("0install-unit-tests"))
            {
                var digest = ManifestUtils.GenerateDigest(testDir, new SilentTaskHandler());
                digest.Sha256New.Should().NotBeNull();
            }
        }
    }
}
