// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using FluentAssertions;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;

namespace ZeroInstall.Store.Implementations.Manifests
{
    public class ManifestTest
    {
        [Fact]
        public void FileOrder()
        {
            var manifest = new Manifest(ManifestFormat.Sha256New);
            var topLevel = manifest[""];
            topLevel.Add("x", new ManifestNormalFile("", 0, 0));
            topLevel.Add("y", new ManifestNormalFile("", 0, 0));
            topLevel.Add("Z", new ManifestNormalFile("", 0, 0));
            topLevel.Keys.Should().Equal("Z", "x", "y");
        }

        [Fact]
        public void TotalSize()
        {
            var manifest = new Manifest(ManifestFormat.Sha256New)
            {
                ["dir1"] =
                {
                    ["file"] = new ManifestNormalFile("abc", 123, 3)
                },
                ["dir2"] =
                {
                    ["file"] = new ManifestSymlink("abc", 5)
                }
            };
            manifest.TotalSize.Should().Be(8);
        }

        [Fact]
        public void RemoveRecursive()
        {
            var manifest = new Manifest(ManifestFormat.Sha256New)
            {
                ["dir1"] =
                {
                    ["file"] = new ManifestNormalFile("abc", 123, 3)
                },
                ["dir1/subdir"] =
                {
                    ["file"] = new ManifestNormalFile("abc", 123, 3)
                },
                ["dir1A"] =
                {
                    ["file"] = new ManifestSymlink("abc", 5)
                }
            };
            manifest.RemoveRecursive("dir1");

            manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha256New)
            {
                ["dir1A"] =
                {
                    ["file"] = new ManifestSymlink("abc", 5)
                }
            });
        }

        [Fact]
        public void MoveRecursive()
        {
            var manifest = new Manifest(ManifestFormat.Sha256New)
            {
                ["dir1"] =
                {
                    ["file"] = new ManifestNormalFile("abc", 123, 3)
                },
                ["dir1/subdir"] =
                {
                    ["file"] = new ManifestNormalFile("abc", 123, 3)
                },
                ["dir1A"] =
                {
                    ["file"] = new ManifestSymlink("abc", 5)
                }
            };
            manifest.RenameRecursive("dir1", "dir2");

            manifest.Should().BeEquivalentTo(new Manifest(ManifestFormat.Sha256New)
            {
                ["dir2"] =
                {
                    ["file"] = new ManifestNormalFile("abc", 123, 3)
                },
                ["dir2/subdir"] =
                {
                    ["file"] = new ManifestNormalFile("abc", 123, 3)
                },
                ["dir1A"] =
                {
                    ["file"] = new ManifestSymlink("abc", 5)
                }
            });
        }

        [Fact]
        public void WithOffset()
        {
            var original = new Manifest(ManifestFormat.Sha256)
            {
                ["dir"] =
                {
                    ["file1"] = new ManifestNormalFile("abc123", new DateTime(2000, 1, 1, 1, 0, 1, DateTimeKind.Utc), 10),
                    ["file2"] = new ManifestExecutableFile("abc123", new DateTime(2000, 1, 1, 1, 0, 1, DateTimeKind.Utc), 10)
                }
            };

            var offset = new Manifest(ManifestFormat.Sha256)
            {
                ["dir"] =
                {
                    ["file1"] = new ManifestNormalFile("abc123", new DateTime(2000, 1, 1, 2, 0, 2, DateTimeKind.Utc), 10),
                    ["file2"] = new ManifestExecutableFile("abc123", new DateTime(2000, 1, 1, 2, 0, 2, DateTimeKind.Utc), 10)
                }
            };

            original.WithOffset(TimeSpan.FromHours(1)).Should().BeEquivalentTo(offset);
        }

        /// <summary>
        /// Ensures that Manifest is correctly generated, serialized and deserialized.
        /// </summary>
        [Fact]
        public void SaveLoad()
        {
            var manifest1 = new Manifest(ManifestFormat.Sha1New)
            {
                ["subdir"] =
                {
                    ["file"] = new ManifestNormalFile("abc123", 1337, 3)
                }
            };
            Manifest manifest2;
            using (var tempFile = new TemporaryFile("0install-test-manifest"))
            {
                // Generate manifest, write it to a file and read the file again
                manifest1.Save(tempFile);
                manifest2 = Manifest.Load(tempFile, ManifestFormat.Sha1New);
            }

            // Ensure data stayed the same
            manifest2.Should().BeEquivalentTo(manifest1);
        }

        /// <summary>
        /// Ensures damaged manifest lines are correctly identified.
        /// </summary>
        [Fact]
        public void LoadException()
        {
            Assert.Throws<FormatException>(() => Manifest.Load("test".ToStream(), ManifestFormat.Sha1New));
            Assert.Throws<FormatException>(() => Manifest.Load("test".ToStream(), ManifestFormat.Sha256));
            Assert.Throws<FormatException>(() => Manifest.Load("test".ToStream(), ManifestFormat.Sha256New));
            Manifest.Load("D /test".ToStream(), ManifestFormat.Sha1New);
            Manifest.Load("D /test".ToStream(), ManifestFormat.Sha256);
            Manifest.Load("D /test".ToStream(), ManifestFormat.Sha256New);

            Manifest.Load("F abc123 1200000000 128 test".ToStream(), ManifestFormat.Sha1New);
            Manifest.Load("F abc123 1200000000 128 test".ToStream(), ManifestFormat.Sha256);
            Manifest.Load("F abc123 1200000000 128 test".ToStream(), ManifestFormat.Sha256New);

            Assert.Throws<FormatException>(() => Manifest.Load("F abc123 128 test".ToStream(), ManifestFormat.Sha1New));
            Assert.Throws<FormatException>(() => Manifest.Load("F abc123 128 test".ToStream(), ManifestFormat.Sha256));
            Assert.Throws<FormatException>(() => Manifest.Load("F abc123 128 test".ToStream(), ManifestFormat.Sha256New));
        }

        [Fact] // Ensures that ToXmlString() correctly outputs a serialized form of the manifest.
        public void TestToString()
        {
            var manifest = new Manifest(ManifestFormat.Sha1New)
            {
                ["subdir"] =
                {
                    ["file"] = new ManifestNormalFile("abc123", 1337, 3)
                }
            };

            manifest.ToString().Replace(Environment.NewLine, "\n")
                    .Should().Be("D /subdir\nF abc123 1337 3 file\n");
        }
    }
}
