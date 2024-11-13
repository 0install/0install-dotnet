// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="Run"/>.
/// </summary>
public class RunTest : SelectionTestBase<Run>
{
    private Mock<ICatalogManager> CatalogManagerMock => GetMock<ICatalogManager>();

    [Fact] // Ensures all options are parsed and handled correctly.
    public void TestNormal()
    {
        var selections = ExpectSolve();

        ExpectFetchUncached(selections,
            new() {ID = "id1", ManifestDigest = new(Sha256: "abc"), Version = new("1.0")},
            new() {ID = "id2", ManifestDigest = new(Sha256: "xyz"), Version = new("1.0")});

        var envBuilderMock = GetMock<IEnvironmentBuilder>();
        GetMock<IExecutor>().Setup(x => x.Inject(selections, "Main")).Returns(envBuilderMock.Object);
        envBuilderMock.SetupFluent(x => x.AddWrapper("Wrapper"))
                      .SetupFluent(x => x.AddArguments("--arg1", "--arg2"))
                      .SetupFluent(x => x.SetEnvironmentVariable(ZeroInstallEnvironment.FeedUriName, "http://example.com/test1.xml"))
                      .SetupFluent(x => x.SetEnvironmentVariable(ZeroInstallEnvironment.CliName, It.IsAny<string>()))
                      .SetupFluent(x => x.SetEnvironmentVariable(ZeroInstallEnvironment.ExternalFetcherName, It.IsAny<string>()))
                      .Setup(x => x.Start()).Returns((Process)null);

        RunAndAssert(null, 0, selections,
            "--command=command", "--os=Windows", "--cpu=i586", "--not-before=1.0", "--before=2.0", "--version-for=http://example.com/test2.xml", "2.0..!3.0",
            "--main=Main", "--wrapper=Wrapper", "http://example.com/test1.xml", "--arg1", "--arg2");
    }

    [Fact] // Ensures local Selections XMLs are correctly detected and parsed.
    public void TestImportSelections()
    {
        var selections = Fake.Selections;

        ExpectFetchUncached(selections,
            new() {ID = "id1", ManifestDigest = new(Sha256: "abc"), Version = new("1.0")},
            new() {ID = "id2", ManifestDigest = new(Sha256: "xyz"), Version = new("1.0")});

        var envBuilderMock = GetMock<IEnvironmentBuilder>();
        GetMock<IExecutor>().Setup(x => x.Inject(selections, null)).Returns(envBuilderMock.Object);
        envBuilderMock.SetupFluent(x => x.AddWrapper(null))
                      .SetupFluent(x => x.AddArguments("--arg1", "--arg2"))
                      .SetupFluent(x => x.SetEnvironmentVariable(ZeroInstallEnvironment.FeedUriName, Fake.Selections.InterfaceUri.ToStringRfc()))
                      .SetupFluent(x => x.SetEnvironmentVariable(ZeroInstallEnvironment.CliName, It.IsAny<string>()))
                      .SetupFluent(x => x.SetEnvironmentVariable(ZeroInstallEnvironment.ExternalFetcherName, It.IsAny<string>()))
                      .Setup(x => x.Start()).Returns((Process)null);

        using var tempFile = new TemporaryFile("0install-test-selections");
        selections.SaveXml(tempFile);

        selections.Normalize();
        RunAndAssert(null, 0, selections, tempFile, "--arg1", "--arg2");
    }

    public override void ShouldRejectTooManyArgs()
    {
        // Not applicable
    }

    [Fact]
    public void GetCanonicalUriRemote()
        => Sut.GetCanonicalUri("http://example.com/test1.xml")
              .ToStringRfc()
              .Should().Be("http://example.com/test1.xml");

    [Fact]
    public void GetCanonicalUriFileAbsolute()
    {
        if (WindowsUtils.IsWindows)
        {
            Sut.GetCanonicalUri(@"C:\test\file").ToStringRfc().Should().Be(@"C:\test\file");
            Sut.GetCanonicalUri(@"file:///C:\test\file").ToStringRfc().Should().Be(@"C:\test\file");
            Sut.GetCanonicalUri("file:///C:/test/file").ToStringRfc().Should().Be(@"C:\test\file");
        }
        if (UnixUtils.IsUnix)
        {
            Sut.GetCanonicalUri("/test/file").ToStringRfc().Should().Be("/test/file");
            Sut.GetCanonicalUri("file:///test/file").ToStringRfc().Should().Be("/test/file");
        }
    }

    [Fact]
    public void GetCanonicalUriFileRelative()
    {
        Sut.FeedManager.Refresh = true;
        CatalogManagerMock.Setup(x => x.GetOnline()).Returns(new Catalog());

        Sut.GetCanonicalUri(Path.Combine("test", "file")).ToString().Should().Be(
            Path.Combine(Environment.CurrentDirectory, "test", "file"));
        Sut.GetCanonicalUri("file:test/file").ToString().Should().Be(
            Path.Combine(Environment.CurrentDirectory, "test", "file"));
    }

    [Fact]
    public void GetCanonicalUriFileInvalid()
    {
        Assert.Throws<UriFormatException>(() => Sut.GetCanonicalUri("file:/test/file"));
    }

    [Fact]
    public void GetCanonicalUriAliases()
    {
        // Fake an alias
        new AppList
        {
            Entries =
            {
                new()
                {
                    InterfaceUri = Fake.Feed1Uri,
                    Name = "Test",
                    AccessPoints = new() {Entries = {new AppAlias {Name = "test"}}}
                }
            }
        }.SaveXml(AppList.GetDefaultPath());

        Sut.GetCanonicalUri("alias:test").Should().Be(Fake.Feed1Uri);
        Assert.Throws<UriFormatException>(() => Sut.GetCanonicalUri("alias:invalid"));
    }

    [Fact]
    public void GetCanonicalUriCatalogCached()
    {
        CatalogManagerMock.Setup(x => x.TryGetCached()).Returns(new Catalog {Feeds = {new() {Uri = Fake.Feed1Uri, Name = "MyApp"}}});
        Sut.GetCanonicalUri("MyApp").Should().Be(Fake.Feed1Uri);
    }

    [Fact]
    public void GetCanonicalUriCatalogOnline()
    {
        Sut.FeedManager.Refresh = true;
        CatalogManagerMock.Setup(x => x.GetOnline()).Returns(new Catalog {Feeds = {new() {Uri = Fake.Feed1Uri, Name = "MyApp"}}});
        Sut.GetCanonicalUri("MyApp").Should().Be(Fake.Feed1Uri);
    }

    [Fact]
    public void KioskModeOK()
    {
        Sut.Config.KioskMode = true;
        CatalogManagerMock.Setup(x => x.TryGetCached()).Returns(new Catalog {Feeds = {new() {Uri = Fake.Feed1Uri, Name = "MyApp"}}});

        TestNormal();
    }

    [Fact]
    public void KioskModeReject()
    {
        Sut.Config.KioskMode = true;
        CatalogManagerMock.Setup(x => x.TryGetCached()).Returns(new Catalog());
        CatalogManagerMock.Setup(x => x.GetOnline()).Returns(new Catalog());

        Assert.Throws<WebException>(() => Sut.Parse(["http://example.com/test1.xml"]));
    }
}
