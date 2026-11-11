// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="Materialize"/>.
/// </summary>
public class MaterializeTest : SelectionTestBase<Materialize>
{
    [Fact]
    public override void ShouldRejectTooManyArgs()
        => Assert.Throws<OptionException>(() => Sut.Parse(["http://example.com/test1.xml", "dir", "arg1"]));

    [Theory]
    [InlineData("Linux", "my-app", false, false)]
    [InlineData("Windows", "my-app.cmd", false, false)]
    [InlineData("Linux", "my-app", true, false)]
    [InlineData("Linux", "my-app", false, true)]
    public void ShouldMaterialize(string os, string launcherName, bool link, bool clean)
    {
        using var tempDir = new TemporaryDirectory("0install-test-materialize");
        string targetDir = Path.Combine(tempDir, "target");
        string staleFile = Path.Combine(targetDir, "bin", "stale");
        Directory.CreateDirectory(Path.GetDirectoryName(staleFile)!);
        File.WriteAllText(staleFile, "from an earlier run");
        var interfaceUri = new FeedUri("http://example.com/my-app.xml");

        var implementation = CreateImplementation(Path.Combine(tempDir, "store"), interfaceUri);
        var selections = new Selections
        {
            InterfaceUri = interfaceUri,
            Command = Command.NameRun,
            Implementations = {implementation}
        };

        Requirements? actualRequirements = null;
        GetMock<ISolver>().Setup(x => x.Solve(It.IsAny<Requirements>()))
                          .Callback((Requirements requirements) => actualRequirements = requirements)
                          .Returns(selections);
        var feed = new Feed {Uri = interfaceUri, Name = "My App"};
        GetMock<IFeedCache>().Setup(x => x.GetFeed(interfaceUri)).Returns(feed);
        ExpectListUncached(selections);

        string[] args = [interfaceUri.ToStringRfc(), targetDir, $"--os={os}", ..link ? ["--link"] : Array.Empty<string>(), ..clean ? ["--clean"] : Array.Empty<string>()];
        RunAndAssert(
            string.Format(Resources.AllComponentsMaterialized, feed.Name, targetDir, Path.Combine(targetDir, launcherName)), ExitCode.OK,
            args);

        actualRequirements!.NoPackages.Should().BeFalse(because: "native packages are only excluded when requested");
        File.Exists(Path.Combine(targetDir, launcherName)).Should().BeTrue();
        File.Exists(Path.Combine(targetDir, implementation.ID, "app.sh")).Should().BeTrue();
        new DirectoryInfo(Path.Combine(targetDir, implementation.ID)).IsDirectoryLink().Should().Be(link);
        File.Exists(staleFile).Should().Be(!clean, because: "content from earlier runs is only removed with --clean");
    }

    /// <summary>
    /// Creates an implementation in a store directory, named after its manifest digest, and registers it with the <see cref="IImplementationStore"/> mock.
    /// </summary>
    private ImplementationSelection CreateImplementation(string storeDir, FeedUri interfaceUri)
    {
        string buildDir = Path.Combine(storeDir, "build");
        Directory.CreateDirectory(buildDir);
        File.WriteAllText(Path.Combine(buildDir, "app.sh"), "#!/bin/sh\necho hello\n");
        ImplFileUtils.SetExecutable(Path.Combine(buildDir, "app.sh"));

        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        new SilentTaskHandler().RunTask(new ReadDirectory(buildDir, builder));
        string digest = builder.Manifest.CalculateDigest();
        string implementationDir = Path.Combine(storeDir, digest);
        Directory.Move(buildDir, implementationDir);
        builder.Manifest.Save(Path.Combine(implementationDir, Manifest.ManifestFile));

        var implementation = new ImplementationSelection
        {
            InterfaceUri = interfaceUri,
            ID = digest,
            Version = new("1.0"),
            ManifestDigest = new(digest),
            Commands = {new() {Name = Command.NameRun, Path = "app.sh"}}
        };
        GetMock<IImplementationStore>().Setup(x => x.GetPath(implementation.ManifestDigest)).Returns(implementationDir);
        return implementation;
    }

    [Theory]
    [InlineData("https://apps.0install.net/gnu/hello.xml", "hello")]
    [InlineData("https://example.com/my%20app/feed", "feed")]
    [InlineData("https://example.com/my%20app.xml", "my app")]
    [InlineData("https://example.com/", "run")]
    public void GetLauncherName(string uri, string expected)
        => Materialize.GetLauncherName(new(uri)).Should().Be(expected);
}
