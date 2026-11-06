// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;

namespace ZeroInstall.Bootstrap;

public class BootstrapConfigTest
{
    private static BootstrapConfig Parse(string ini)
        => BootstrapConfig.Parse(ini.ToStream());

    [Fact]
    public void ParsesValues()
    {
        var config = Parse("""
            [bootstrap]
            key_fingerprint=ABC
            app_uri=https://example.com/app.xml
            app_name=My App
            app_args=--foo --bar
            integrate_args=--add-standard
            catalog_uri=https://example.com/catalog.xml
            customizable_store_path=TRUE
            estimated_required_space=1024
            """);

        config.KeyFingerprint.Should().Be("ABC");
        config.AppUri.Should().Be(new FeedUri("https://example.com/app.xml"));
        config.AppName.Should().Be("My App");
        config.AppArgs.Should().Be("--foo --bar");
        config.IntegrateArgs.Should().Be("--add-standard");
        config.CatalogUri.Should().Be(new FeedUri("https://example.com/catalog.xml"));
        config.CustomizableStorePath.Should().BeTrue();
        config.EstimatedRequiredSpace.Should().Be(1024);
    }

    [Fact]
    public void TreatsEmptyAndPlaceholderValuesAsUnset()
    {
        var config = Parse("""
            [bootstrap]
            app_uri=;-----AppUri-----
            app_name=
            """);

        config.AppUri.Should().BeNull();
        config.AppName.Should().BeNull();
        config.CustomizableStorePath.Should().BeFalse();
        config.EstimatedRequiredSpace.Should().BeNull();
    }

    [Theory]
    [InlineData("estimated_required_space=abc")]
    [InlineData("estimated_required_space=99999999999999999999")]
    [InlineData("app_uri=not a uri")]
    public void RejectsInvalidValues(string line)
    {
        var parse = () => Parse($"[bootstrap]\n{line}");
        parse.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void EmbeddedConfigHasStableResourceNameAndNoApp()
    {
        // 0bootstrap replaces this resource by name, so it must not change with the namespace
        using var stream = typeof(BootstrapConfig).Assembly.GetManifestResourceStream("ZeroInstall.BootstrapConfig.ini");
        stream.Should().NotBeNull();

        var config = BootstrapConfig.Parse(stream!);
        config.AppUri.Should().BeNull();
        config.AppName.Should().BeNull();
        config.IntegrateArgs.Should().BeNull();
    }
}
