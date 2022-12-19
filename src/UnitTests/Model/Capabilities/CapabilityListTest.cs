// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Contains test methods for <see cref="CapabilityList"/>.
/// </summary>
public sealed class CapabilityListTest
{
    #region Helpers
    /// <summary>
    /// Creates a fictive test <see cref="CapabilityList"/>.
    /// </summary>
    public static CapabilityList CreateTestCapabilityList()
    {
        var testIcon = new Icon {Href = new("http://example.com/icons/test.ico"), MimeType = "image/vnd.microsoft.icon"};
        var testVerb = new Verb {Name = Verb.NameOpen, Descriptions = {"Verb description"}, Command = Command.NameRun, Arguments = {"--open"}};
        return new()
        {
            OS = Architecture.CurrentSystem.OS,
            Entries =
            {
                new AppRegistration {ID = "myapp", CapabilityRegPath = @"SOFTWARE\MyApp\Capabilities"},
                new AutoPlay {ID = "autoplay", Descriptions = {"Do something"}, Icons = {testIcon}, Provider = "MyApp", Verb = testVerb, Events = {new AutoPlayEvent {Name = AutoPlayEvent.NameBurnCD}}},
                new ComServer {ID = "com-server"},
                new ContextMenu {ID = "context-menu", Verbs = {testVerb}},
                new DefaultProgram {ID = "default-program", Descriptions = {"My mail client"}, Icons = {testIcon}, Verbs = {testVerb}, Service = "Mail", InstallCommands = new() {ShowIcons = "helper.exe --show", HideIcons = "helper.exe --hide", Reinstall = "helper.exe --reinstall.exe"}},
                new FileType {ID = "my_ext1", Descriptions = {"Text file"}, Icons = {testIcon}, Extensions = {new() {Value = "txt", MimeType = "text/plain"}}, Verbs = {testVerb}},
                new FileType {ID = "my_ext2", Descriptions = {"JPG image"}, Icons = {testIcon}, Extensions = {new() {Value = "jpg", MimeType = "image/jpg"}}, Verbs = {testVerb}},
                new UrlProtocol {ID = "my_protocol", Descriptions = {"My protocol"}, Icons = {testIcon}, Verbs = {testVerb}, KnownPrefixes = {new() {Value = "my-protocol"}}}
            }
        };
    }
    #endregion

    [Fact] // Ensures that the class is correctly serialized and deserialized.
    public void TestSaveLoad()
    {
        CapabilityList capabilityList1 = CreateTestCapabilityList(), capabilityList2;
        using (var tempFile = new TemporaryFile("0install-test-capabilities"))
        {
            // Write and read file
            capabilityList1.SaveXml(tempFile);
            capabilityList2 = XmlStorage.LoadXml<CapabilityList>(tempFile);
        }

        // Ensure data stayed the same
        capabilityList2.Should().Be(capabilityList1, because: "Serialized objects should be equal.");
        capabilityList2.GetHashCode().Should().Be(capabilityList1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
        capabilityList2.Should().NotBeSameAs(capabilityList1, because: "Serialized objects should not return the same reference.");
    }

    [Fact] // Ensures that the class can be correctly cloned.
    public void TestClone()
    {
        var capabilityList1 = CreateTestCapabilityList();
        var capabilityList2 = capabilityList1.Clone();

        // Ensure data stayed the same
        capabilityList2.Should().Be(capabilityList1, because: "Cloned objects should be equal.");
        capabilityList2.GetHashCode().Should().Be(capabilityList1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        capabilityList2.Should().NotBeSameAs(capabilityList1, because: "Cloning should not return the same reference.");
    }
}
