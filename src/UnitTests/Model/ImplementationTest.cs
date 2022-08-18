// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="Implementation"/>.
/// </summary>
public class ImplementationTest
{
    /// <summary>
    /// Creates a fictive test <see cref="Implementation"/>.
    /// </summary>
    public static Implementation CreateTestImplementation() => new()
    {
        ID = "id",
        ManifestDigest = new ManifestDigest(Sha256: "123"),
        Version = new("1.0"),
        Architecture = new(OS.Windows, Cpu.I586),
        Languages = {"en-US"},
        Main = "executable",
        DocDir = "doc",
        Stability = Stability.Developer,
        Bindings = {EnvironmentBindingTest.CreateTestBinding()},
        RetrievalMethods = {ArchiveTest.CreateTestArchive(), new Recipe {Steps = {ArchiveTest.CreateTestArchive()}}},
        Commands = {CommandTest.CreateTestCommand1()}
    };

    /// <summary>
    /// Ensures that <see cref="Element.ContainsCommand"/> correctly checks for commands.
    /// </summary>
    [Fact]
    public void ContainsCommand()
    {
        var implementation = CreateTestImplementation();
        implementation.ContainsCommand(Command.NameRun).Should().BeTrue();
        implementation.ContainsCommand("other-command").Should().BeFalse();
    }

    /// <summary>
    /// Ensures that <see cref="Element.GetCommand"/> and <see cref="Element.this"/> correctly retrieve commands.
    /// </summary>
    [Fact]
    [SuppressMessage("ReSharper", "UnusedVariable")]
    public void TestGetCommand()
    {
        var implementation = CreateTestImplementation();

        implementation.GetCommand(Command.NameRun).Should().Be(implementation.Commands[0]);
        implementation[Command.NameRun].Should().Be(implementation.Commands[0]);

        Assert.Throws<ArgumentNullException>(() => implementation.GetCommand(""));
        implementation[""].Should().BeNull();

        implementation.GetCommand("invalid").Should().BeNull();
        Assert.Throws<KeyNotFoundException>(() => implementation["invalid"]);
    }

    /// <summary>
    /// Ensures that <see cref="Implementation.Normalize"/> correctly identifies manifest digests in the ID tag.
    /// </summary>
    [Fact]
    public void NormalizeID()
    {
        var implementation = new Implementation {ID = "sha256=123", Version = new("1.0")};
        implementation.Normalize(FeedTest.Test1Uri);
        implementation.ManifestDigest.Sha256.Should().Be("123");

        implementation = new Implementation {ID = "sha256=wrong", Version = new("1.0"), ManifestDigest = new ManifestDigest(Sha256: "correct")};
        implementation.Normalize(FeedTest.Test1Uri);
        implementation.ManifestDigest.Sha256.Should().Be("correct");

        implementation = new Implementation {ID = "abc", Version = new("1.0")};
        implementation.Normalize(FeedTest.Test1Uri);
    }

    /// <summary>
    /// Ensures that <see cref="Implementation.Normalize"/> correctly converts <see cref="Element.Main"/> and <see cref="Element.SelfTest"/> to <see cref="Command"/>s.
    /// </summary>
    [Fact]
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public void TestNormalizeCommand()
    {
        var implementation = new Implementation
        {
            ID = "abc", Version = new("1.0"),
            Main = "main",
            SelfTest = "test"
        };
        implementation.Normalize(FeedTest.Test1Uri);
        implementation[Command.NameRun].Path.Should().Be("main");
        implementation[Command.NameTest].Path.Should().Be("test");
    }

    /// <summary>
    /// Ensures that <see cref="Implementation.Normalize"/> correctly makes <see cref="ImplementationBase.LocalPath"/> absolute.
    /// </summary>
    [Fact]
    public void NormalizeLocalPath()
    {
        var localUri = new FeedUri(WindowsUtils.IsWindows ? @"C:\local\feed.xml" : "/local/feed.xml");

        ImplementationVersion version = new("1.0");
        var implementation1 = new Implementation {ID = "./subdir", Version = version};
        implementation1.Normalize(localUri);
        implementation1.ID.Should().Be(WindowsUtils.IsWindows ? @"C:\local\.\subdir" : "/local/./subdir");
        implementation1.LocalPath.Should().Be(WindowsUtils.IsWindows ? @"C:\local\.\subdir" : "/local/./subdir");

        var implementation2 = new Implementation {ID = "./wrong", Version = version, LocalPath = "subdir"};
        implementation2.Normalize(localUri);
        implementation2.ID.Should().Be("./wrong");
        implementation2.LocalPath.Should().Be(WindowsUtils.IsWindows ? @"C:\local\subdir" : "/local/subdir");

        if (UnixUtils.IsUnix)
        {
            var implementation3 = new Implementation {ID = "/root", Version = version};
            implementation3.Normalize(localUri);
            implementation3.ID.Should().Be("/root");
            implementation3.LocalPath.Should().Be("/root");
        }
    }

    /// <summary>
    /// Ensures that <see cref="Implementation.Normalize"/> rejects local paths in non-local feeds.
    /// </summary>
    [Fact]
    public void NormalizeRejectLocalPath()
    {
        var implementation = new Implementation
        {
            ID = "abc", Version = new("1.0"),
            LocalPath = "subdir"
        };
        implementation.Normalize(FeedTest.Test1Uri);
        implementation.LocalPath.Should().BeNull();
    }

    /// <summary>
    /// Ensures that <see cref="Implementation.Normalize"/> rejects relative <see cref="DownloadRetrievalMethod.Href"/>s in non-local feeds.
    /// </summary>
    [Fact]
    public void NormalizeRejectRelativeHref()
    {
        var relative = new Archive {Href = new("relative", UriKind.Relative)};
        var absolute = new Archive {Href = new("http://server/absolute.zip", UriKind.Absolute)};
        var implementation = new Implementation
        {
            ID = "abc", Version = new("1.0"),
            RetrievalMethods = {relative, absolute}
        };

        implementation.Normalize(FeedTest.Test1Uri);
        implementation.RetrievalMethods.Should().Equal(absolute);
    }

    /// <summary>
    /// Ensures that the class can be correctly cloned.
    /// </summary>
    [Fact]
    public void Clone()
    {
        var implementation1 = CreateTestImplementation();
        var implementation2 = implementation1.CloneImplementation();

        // Ensure data stayed the same
        implementation2.Should().Be(implementation1, because: "Cloned objects should be equal.");
        implementation2.GetHashCode().Should().Be(implementation1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        implementation2.Should().NotBeSameAs(implementation1, because: "Cloning should not return the same reference.");
    }
}
