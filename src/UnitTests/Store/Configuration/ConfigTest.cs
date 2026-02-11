// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Threading;

namespace ZeroInstall.Store.Configuration;

/// <summary>
/// Contains test methods for <see cref="Config"/>.
/// </summary>
public class ConfigTest : TestWithRedirect
{
    /// <summary>
    /// Creates test <see cref="Config"/>.
    /// </summary>
    public static Config CreateTestConfig() => new()
    {
        HelpWithTesting = true,
        Freshness = TimeSpan.FromHours(12),
        NetworkUse = NetworkLevel.Minimal,
        AutoApproveKeys = false,
        SyncServerPassword = "pw123"
    };

    [Fact] // Ensures that the class can be correctly cloned.
    public void TestClone()
    {
        var config1 = CreateTestConfig();
        var config2 = config1.Clone();

        // Ensure data stayed the same
        config2.Should().Equal(config1, because: "Cloned objects should be equal.");
        config2.GetHashCode().Should().Be(config1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        config2.Should().NotBeSameAs(config1, because: "Cloning should not return the same reference.");
    }

    /// <summary>
    /// Ensures that the class is correctly serialized and deserialized.
    /// </summary>
    [Fact]
    public void SaveLoad()
    {
        var config1 = CreateTestConfig();
        var config2 = new Config();
        using (var tempFile = new TemporaryFile("0install-test-config"))
        {
            // Write and read file
            config1.Save(tempFile);
            config2.ReadFromFile(tempFile);
        }

        // Ensure data stayed the same
        config2.Should().Equal(config1, because: "Serialized objects should be equal.");
        config2.GetHashCode().Should().Be(config1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
        config2.Should().NotBeSameAs(config1, because: "Serialized objects should not return the same reference.");
    }

    /// <summary>
    /// Ensures <see cref="Config.GetOption"/> and <see cref="Config.SetOption"/> properly access the settings properties.
    /// </summary>
    [Fact]
    public void GetSetValue()
    {
        var config = new Config();
        Assert.Throws<KeyNotFoundException>(() => config.SetOption("Test", "Test"));

        config.HelpWithTesting.Should().BeFalse();
        config.GetOption("help_with_testing").Should().Be("False");
        config.SetOption("help_with_testing", "True");
        Assert.Throws<FormatException>(() => config.SetOption("help_with_testing", "Test"));
        config.HelpWithTesting.Should().BeTrue();
        config.GetOption("help_with_testing").Should().Be("True");

        config.SetOption("freshness", "10");
        config.Freshness.Should().Be(TimeSpan.FromSeconds(10));
        config.GetOption("freshness").Should().Be("10");
    }

    /// <summary>
    /// Ensures <see cref="Config.Save(string)"/> preserves unknown properties loaded in <see cref="Config.ReadFromFile(string)"/>.
    /// </summary>
    [Fact]
    public void RetainUnknownProperties()
    {
        string testIniData = $"[global]{Environment.NewLine}test = test{Environment.NewLine}";

        using var tempFile = new TemporaryFile("0install-test-config");
        File.WriteAllText(tempFile, testIniData);

        var config = new Config();
        config.ReadFromFile(tempFile);
        config.Save(tempFile);

        File.ReadAllText(tempFile).Should().Be(testIniData);
    }

    [Fact]
    public void LoadStressTest()
    {
        new Config().Save();

        StressTest.Run(() => _ = Config.Load());
    }

    /// <summary>
    /// Ensures that user config can override machine-wide config back to default values.
    /// </summary>
    [Fact]
    public void OverrideMachineWideWithDefault()
    {
        using var machineWideFile = new TemporaryFile("0install-test-machine-config");
        using var userFile = new TemporaryFile("0install-test-user-config");

        // Machine-wide config sets a non-default value
        var machineWideConfig = new Config { NetworkUse = NetworkLevel.Minimal };
        machineWideConfig.Save(machineWideFile);

        // User wants to override back to default (Full)
        var userConfig = new Config();
        userConfig.ReadFromFile(machineWideFile); // Load machine-wide first
        userConfig.SetOption("network_use", "full"); // Explicitly override to default
        userConfig.Save(userFile);

        // Load both configs (machine-wide first, then user)
        var loadedConfig = new Config();
        loadedConfig.ReadFromFile(machineWideFile);
        loadedConfig.ReadFromFile(userFile);

        // User's override should be respected
        loadedConfig.NetworkUse.Should().Be(NetworkLevel.Full,
            because: "User config should be able to override machine-wide config back to default value");
    }

    /// <summary>
    /// Ensures that default values are not saved when no override is needed.
    /// </summary>
    [Fact]
    public void DefaultValuesNotSavedWithoutOverride()
    {
        using var tempFile = new TemporaryFile("0install-test-config");

        // Create config with all default values
        var config = new Config();
        config.Save(tempFile);

        // The file should be minimal (only contain the section header)
        string contents = File.ReadAllText(tempFile);
        contents.Should().NotContain("network_use",
            because: "Default values should not be saved when not explicitly set");
        contents.Should().NotContain("help_with_testing",
            because: "Default values should not be saved when not explicitly set");
    }

    /// <summary>
    /// Ensures that explicitly set default values persist across save/load cycles.
    /// </summary>
    [Fact]
    public void ExplicitDefaultValuesPersist()
    {
        using var tempFile = new TemporaryFile("0install-test-config");

        // Explicitly set a value to its default using SetOption
        var config1 = new Config();
        config1.SetOption("help_with_testing", "false"); // Explicitly set to default
        config1.Save(tempFile);

        // The explicitly set default should be in the file
        string contents = File.ReadAllText(tempFile);
        contents.Should().Contain("help_with_testing",
            because: "Explicitly set default values should be saved");

        // Load it back
        var config2 = new Config();
        config2.ReadFromFile(tempFile);

        // Save again
        config2.Save(tempFile);

        // The value should still be in the file
        string contents2 = File.ReadAllText(tempFile);
        contents2.Should().Contain("help_with_testing",
            because: "Explicitly set default values should persist across save/load cycles");
    }

    /// <summary>
    /// Ensures that ResetOption removes the explicit override, allowing inheritance from machine-wide config.
    /// </summary>
    [Fact]
    public void ResetOptionRemovesOverride()
    {
        using var tempFile = new TemporaryFile("0install-test-config");

        // Explicitly set a value
        var config1 = new Config();
        config1.SetOption("help_with_testing", "true");
        config1.Save(tempFile);

        // Verify it's saved
        File.ReadAllText(tempFile).Should().Contain("help_with_testing");

        // Load it back and reset
        var config2 = new Config();
        config2.ReadFromFile(tempFile);
        config2.ResetOption("help_with_testing");
        config2.Save(tempFile);

        // The reset should remove it from the file (since it's now at default)
        string contents = File.ReadAllText(tempFile);
        contents.Should().NotContain("help_with_testing",
            because: "ResetOption should remove the explicit override");
    }
}
