// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Windows;

public class BrowserNativeMessagingManifestTest
{
    [Fact]
    public void TestJsonSerialization()
    {
        new BrowserNativeMessagingManifest(Name: "my-name", Description: "short description", Path: "some/path")
           .ToJsonString().Should().Be(
                """
                {"name":"my-name","description":"short description","path":"some/path","type":"stdio"}
                """);
    }
}
