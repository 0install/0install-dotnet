// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Bootstrap;

public class BootstrapProcessConfigTest : TestWithRedirect
{
    private static readonly FeedUri _customSelfUpdateUri = new("https://example.com/custom.xml");

    private static BootstrapConfig Bootstrap => BootstrapConfig.Parse("[global]\nkiosk_mode=true".ToStream());

    private static Config LoadUserFile()
    {
        var config = new Config();
        config.ReadFromFilesUserOnly();
        return config;
    }

    [Fact]
    public void SaveCreatesUserFile()
    {
        BootstrapProcess.SaveBootstrapConfig(Bootstrap.IniData, selfUpdateUri: null, machineWide: false);

        LoadUserFile().KioskMode.Should().BeTrue();
    }

    [Fact]
    public void SaveWritesSelfUpdateUri()
    {
        BootstrapProcess.SaveBootstrapConfig(Bootstrap.IniData, _customSelfUpdateUri, machineWide: false);

        LoadUserFile().SelfUpdateUri.Should().Be(_customSelfUpdateUri);
    }

    [Fact]
    public void SaveLetsSelfUpdateUriOverrideBootstrapConfig()
    {
        var bootstrap = BootstrapConfig.Parse("[global]\nself_update_uri=https://example.com/bootstrap.xml".ToStream());

        BootstrapProcess.SaveBootstrapConfig(bootstrap.IniData, _customSelfUpdateUri, machineWide: false);

        LoadUserFile().SelfUpdateUri.Should().Be(_customSelfUpdateUri);
    }

    [Fact]
    public void SaveKeepsExistingUserOptions()
    {
        new Config {HelpWithTesting = true}.Save();

        BootstrapProcess.SaveBootstrapConfig(Bootstrap.IniData, selfUpdateUri: null, machineWide: false);

        var config = LoadUserFile();
        config.HelpWithTesting.Should().BeTrue();
        config.KioskMode.Should().BeTrue();
    }

    [Fact]
    public void SaveLetsBootstrapConfigOverrideUserOptions()
    {
        new Config {KioskMode = false, SelfUpdateUri = _customSelfUpdateUri}.Save();

        BootstrapProcess.SaveBootstrapConfig(Bootstrap.IniData, selfUpdateUri: null, machineWide: false);

        var config = LoadUserFile();
        config.KioskMode.Should().BeTrue();
        config.SelfUpdateUri.Should().Be(_customSelfUpdateUri);
    }
}
