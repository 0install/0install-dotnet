// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Preferences;

/// <summary>
/// Contains test methods for <see cref="InterfacePreferences"/>.
/// </summary>
public class InterfacePreferencesTest
{
    /// <summary>
    /// Creates a fictive test <see cref="InterfacePreferences"/>.
    /// </summary>
    public static InterfacePreferences CreateTestInterfacePreferences() => new()
    {
        Uri = new("http://somedomain/someapp.xml"),
        StabilityPolicy = Stability.Testing,
        Feeds = {new() {Source = new("http://invalid/")}}
    };

    [Fact] // Ensures that the class is correctly serialized and deserialized.
    public void TestSaveLoad()
    {
        InterfacePreferences preferences1 = CreateTestInterfacePreferences(), preferences2;
        using (var tempFile = new TemporaryFile("0install-test-prefs"))
        {
            // Write and read file
            preferences1.SaveXml(tempFile);
            preferences2 = XmlStorage.LoadXml<InterfacePreferences>(tempFile);
        }

        // Ensure data stayed the same
        preferences2.Should().Be(preferences1, because: "Serialized objects should be equal.");
        preferences2.GetHashCode().Should().Be(preferences1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
        preferences2.Should().NotBeSameAs(preferences1, because: "Serialized objects should not return the same reference.");
    }

    [Fact] // Ensures that the class can be correctly cloned.
    public void TestClone()
    {
        var preferences1 = CreateTestInterfacePreferences();
        var preferences2 = preferences1.Clone();

        // Ensure data stayed the same
        preferences2.Should().Be(preferences1, because: "Cloned objects should be equal.");
        preferences2.GetHashCode().Should().Be(preferences1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        ReferenceEquals(preferences1, preferences2).Should().BeFalse(because: "Cloning should not return the same reference.");
    }
}
