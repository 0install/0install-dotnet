// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Contains test methods for <see cref="MimeAppsList"/>.
/// </summary>
public class MimeAppsListTest
{
    private static string Edit(string content, Action<MimeAppsList> action)
    {
        var list = new MimeAppsList(content.Split(['\n'], StringSplitOptions.RemoveEmptyEntries));
        action(list);
        return list.ToString();
    }

    [Fact]
    public void AddToEmpty()
        => Edit("", x => x.AddDefault("text/plain", "a.desktop"))
           .Should().Be("[Default Applications]\ntext/plain=a.desktop;\n");

    [Fact]
    public void AddPreservesOthers()
        => Edit("# Comment\n[Default Applications]\ntext/html=b.desktop\n[Added Associations]\ntext/plain=c.desktop;\n", x => x.AddDefault("text/plain", "a.desktop"))
           .Should().Be("# Comment\n[Default Applications]\ntext/html=b.desktop\ntext/plain=a.desktop;\n[Added Associations]\ntext/plain=c.desktop;\n");

    [Fact]
    public void AddAndRemoveRestoresPrevious()
    {
        string added = Edit("[Default Applications]\ntext/plain=b.desktop;\n", x => x.AddDefault("text/plain", "a.desktop"));
        added.Should().Be("[Default Applications]\ntext/plain=a.desktop;b.desktop;\n");

        Edit(added, x => x.AddDefault("text/plain", "a.desktop")).Should().Be(added);
        Edit(added, x => x.RemoveDefault("text/plain", "a.desktop")).Should().Be("[Default Applications]\ntext/plain=b.desktop;\n");
    }

    [Fact]
    public void RemoveDefaultOnlyAffectsDefaults()
        => Edit("[Default Applications]\ntext/plain=a.desktop;\ntext/html=a.desktop;\n[Added Associations]\ntext/plain=a.desktop;\n", x => x.RemoveDefault("text/plain", "a.desktop"))
           .Should().Be("[Default Applications]\ntext/html=a.desktop;\n[Added Associations]\ntext/plain=a.desktop;\n");

    [Fact]
    public void RemoveAll()
        => Edit("[Default Applications]\ntext/plain=a.desktop;\ntext/html=a.desktop;b.desktop;\n[Added Associations]\ntext/plain=a.desktop;\n", x => x.RemoveAll("a.desktop"))
           .Should().Be("[Default Applications]\ntext/html=b.desktop;\n[Added Associations]\n");
}
