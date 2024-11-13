// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Contains integration tests for <see cref="ListApps"/>.
/// </summary>
public class ListAppsTest : CliCommandTestBase<ListApps>
{
    private static readonly AppEntry
        _app1 = new() { Name = "App 1", InterfaceUri = new("http://example.com/app1.xml") },
        _app2 = new() { Name = "App 2", InterfaceUri = new("http://example.com/app2.xml") };

    public ListAppsTest()
    {
        new AppList { Entries = { _app1, _app2 } }.SaveXml(AppList.GetDefaultPath());
    }

    [Fact]
    public void ListAll()
    {
        RunAndAssert([_app1, _app2], ExitCode.OK);
    }

    [Fact]
    public void ListAllXml()
    {
        RunAndAssert(new AppList { Entries = { _app1, _app2 } }.ToXmlString(), ExitCode.OK,
            "--xml");
    }

    [Fact]
    public void FilterByUri()
    {
        RunAndAssert([_app2], ExitCode.OK,
            "http://example.com/app2.xml");
    }

    [Fact]
    public void FilterByUriXml()
    {
        RunAndAssert(new AppList { Entries = { _app2 } }.ToXmlString(), ExitCode.OK,
            "--xml", "http://example.com/app2.xml");
    }

    [Fact]
    public void FilterByName()
    {
        RunAndAssert([_app2], ExitCode.OK,
            "app 2");
    }

    [Fact]
    public void FilterByNameXml()
    {
        RunAndAssert(new AppList { Entries = { _app2 } }.ToXmlString(), ExitCode.OK,
            "--xml", "app 2");
    }
}
