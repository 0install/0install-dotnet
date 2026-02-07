// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;

namespace ZeroInstall.Store.Manifests;

public class ManifestTest
{
    [Fact]
    public void SortingDirs()
    {
        var manifest = new Manifest(ManifestFormat.Sha256New) {"b/y", "b-x", "b/x", "b", "a", "Z"};
        manifest.Keys.Should().Equal("", "Z", "a", "b", "b/x", "b/y", "b-x");
    }

    [Fact]
    public void SortingFiles()
    {
        var manifest = new Manifest(ManifestFormat.Sha256New)
        {
            [""] =
            {
                ["x"] = new ManifestNormalFile("", 0, 0),
                ["y"] = new ManifestNormalFile("", 0, 0),
                ["Z"] = new ManifestNormalFile("", 0, 0)
            }
        };
        manifest[""].Keys.Should().Equal("Z", "x", "y");
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
    public void AddRecursive()
    {
        var manifest = new Manifest(ManifestFormat.Sha256New) {"dir1/subdir"};
        manifest.ContainsKey("dir1").Should().BeTrue();
        manifest.ContainsKey("dir1/subdir").Should().BeTrue();
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
        manifest.Remove("dir1");

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
        manifest.Rename("dir1", "dir2");

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

    [Fact]
    public void ScopedTo()
    {
        var original = new Manifest(ManifestFormat.Sha256New)
        {
            [""] =
            {
                ["root-file"] = new ManifestNormalFile("hash1", 100, 10)
            },
            ["dir1"] =
            {
                ["file1"] = new ManifestNormalFile("hash2", 200, 20)
            },
            ["dir1/subdir"] =
            {
                ["file2"] = new ManifestNormalFile("hash3", 300, 30)
            },
            ["dir2"] =
            {
                ["file3"] = new ManifestNormalFile("hash4", 400, 40)
            }
        };

        var expected = new Manifest(ManifestFormat.Sha256New)
        {
            [""] =
            {
                ["file1"] = new ManifestNormalFile("hash2", 200, 20)
            },
            ["subdir"] =
            {
                ["file2"] = new ManifestNormalFile("hash3", 300, 30)
            }
        };

        original.ScopedTo("dir1").Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ScopedToDeep()
    {
        var original = new Manifest(ManifestFormat.Sha256New)
        {
            ["a/b/c"] =
            {
                ["file"] = new ManifestNormalFile("hash", 100, 10)
            }
        };

        var expected = new Manifest(ManifestFormat.Sha256New)
        {
            ["c"] =
            {
                ["file"] = new ManifestNormalFile("hash", 100, 10)
            }
        };

        original.ScopedTo("a/b").Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ScopedToNonExistent()
    {
        var original = new Manifest(ManifestFormat.Sha256New)
        {
            ["dir1"] =
            {
                ["file1"] = new ManifestNormalFile("hash2", 200, 20)
            }
        };

        var expected = new Manifest(ManifestFormat.Sha256New);

        original.ScopedTo("non-existent").Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ScopedToRoot()
    {
        var original = new Manifest(ManifestFormat.Sha256New)
        {
            ["dir1"] =
            {
                ["file1"] = new ManifestNormalFile("hash2", 200, 20)
            }
        };

        original.ScopedTo("").Should().BeEquivalentTo(original);
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
            ["subdir1"] =
            {
                ["fileA"] = new ManifestNormalFile("abc123", 1337, 1),
                ["fileB"] = new ManifestNormalFile("abc123", 1337, 2)
            },
            ["subdir2"] =
            {
                ["fileX"] = new ManifestNormalFile("abc123", 1337, 3),
                ["fileY"] = new ManifestNormalFile("abc123", 1337, 4)
            }
        };

        manifest.ToString()
                .Should().Be("D /subdir1\nF abc123 1337 1 fileA\nF abc123 1337 2 fileB\nD /subdir2\nF abc123 1337 3 fileX\nF abc123 1337 4 fileY\n");
    }

    [Fact]
    public void GetTopLevelDirectories()
    {
        var manifest = new Manifest(ManifestFormat.Sha256New)
        {
            ["dir1"] = { ["file"] = new ManifestNormalFile("hash1", 0, 0) },
            ["dir1/subdir"] = { ["file"] = new ManifestNormalFile("hash2", 0, 0) },
            ["dir2"] = { ["file"] = new ManifestNormalFile("hash3", 0, 0) }
        };

        manifest.GetTopLevelDirectories().Should().BeEquivalentTo("dir1", "dir2");
    }

    [Fact]
    public void GetTopLevelFiles()
    {
        var manifest = new Manifest(ManifestFormat.Sha256New)
        {
            [""] =
            {
                ["file1"] = new ManifestNormalFile("hash1", 0, 0),
                ["file2"] = new ManifestNormalFile("hash2", 0, 0)
            },
            ["dir1"] = { ["file3"] = new ManifestNormalFile("hash3", 0, 0) }
        };

        manifest.GetTopLevelFiles().Should().BeEquivalentTo("file1", "file2");
    }
}
