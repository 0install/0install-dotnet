// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Contains test methods for the name validation of <see cref="CommandAccessPoint"/>s.
/// </summary>
public class CommandAccessPointTest
{
    private static readonly AppEntry _appEntry = new() {InterfaceUri = FeedTest.Test1Uri, Name = "Test"};

    private static void Apply(CommandAccessPoint accessPoint)
        => accessPoint.Apply(_appEntry, new Feed {Name = "Test"}, new Mock<IIconStore>().Object, machineWide: false);

    [Theory]
    [MemberData(nameof(SafeNameTest.InvalidNames), MemberType = typeof(SafeNameTest))]
    public void AliasRejectsInvalidNames(string name)
        => Assert.Throws<IOException>(() => Apply(new AppAlias {Name = name}));

    [Theory]
    [InlineData("")]
    [InlineData("a:b")]
    [InlineData("a?b")]
    [InlineData("a/b")]
    [InlineData("a\tb")]
    [InlineData("CON")]
    [InlineData("nul.txt")]
    public void IconRejectsNonPortableFileNames(string name)
        => Assert.Throws<IOException>(() => Apply(new DesktopIcon {Name = name}));

    [Theory]
    [MemberData(nameof(SafeNameTest.InvalidPaths), MemberType = typeof(SafeNameTest))]
    public void MenuEntryRejectsInvalidCategories(string category)
        => Assert.Throws<IOException>(() => Apply(new MenuEntry {Name = "0install-unit-test", Category = category}));

    [Theory]
    [InlineData("0install-test=alias")]
    [InlineData("-0install-test-alias")]
    public void AliasRemovalIsLenient(string name)
        => new AppAlias {Name = name}.Unapply(_appEntry, machineWide: false);

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("a/b")]
    public void AliasRemovalRejectsUnsafePaths(string name)
        => Assert.Throws<IOException>(() => new AppAlias {Name = name}.Unapply(_appEntry, machineWide: false));
}
