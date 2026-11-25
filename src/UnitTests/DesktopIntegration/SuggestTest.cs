// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains test methods for <see cref="Suggest"/>.
/// </summary>
public class SuggestTest
{
    [Fact]
    public void SingleMenuEntry()
    {
        Suggest.MenuEntries(new Feed
        {
            Name = "My App/",
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun}
            }
        }).Should().Equal(
            new MenuEntry {Name = "My App-", Command = Command.NameRun});
    }

    [Fact]
    public void SingleMenuEntryWithCategory()
    {
        Suggest.MenuEntries(new Feed
        {
            Name = "My App/",
            Categories = {"My Category/"},
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun}
            }
        }).Should().Equal(
            new MenuEntry {Category = "My Category-", Name = "My App-", Command = Command.NameRun});
    }

    [Fact]
    public void MultipleMenuEntries()
    {
        Suggest.MenuEntries(new Feed
        {
            Name = "My App/",
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun},
                new EntryPoint {Command = "extra"}
            }
        }).Should().Equal(
            new MenuEntry {Category = "My App-", Name = "My App-", Command = Command.NameRun},
            new MenuEntry {Category = "My App-", Name = "My App- extra", Command = "extra"});
    }

    [Fact]
    public void MultipleMenuEntriesWithCategory()
    {
        Suggest.MenuEntries(new Feed
        {
            Name = "My App/",
            Categories = {"My Category/"},
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun},
                new EntryPoint {Command = "extra", Names = {"Extra"}}
            }
        }).Should().Equal(
            new MenuEntry {Category = "My Category-/My App-", Name = "My App-", Command = Command.NameRun},
            new MenuEntry {Category = "My Category-/My App-", Name = "Extra", Command = "extra"});
    }

    [Theory]
    [InlineData("My App", "...", "My App")]
    [InlineData("My App", "CON", "My App")]
    [InlineData("0install", "My Category", "My Category")]
    [InlineData("...", "My Category", "My Category")]
    [InlineData("0install", "...", null)]
    public void MenuEntriesSkipInvalidCategories(string name, string category, string? expectedCategory)
    {
        Suggest.MenuEntries(new Feed
        {
            Name = name,
            Categories = {category},
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun, Names = {"Main"}},
                new EntryPoint {Command = "extra", Names = {"Extra"}}
            }
        }).Should().Equal(
            new MenuEntry {Category = expectedCategory, Name = "Main", Command = Command.NameRun},
            new MenuEntry {Category = expectedCategory, Name = "Extra", Command = "extra"});
    }

    [Fact]
    public void DesktopIcons()
    {
        Suggest.DesktopIcons(new Feed
        {
            Name = "My App/",
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun},
                new EntryPoint {Command = "extra", Names = {"Extra"}}
            }
        }).Should().Equal(
            new DesktopIcon {Name = "My App-", Command = Command.NameRun});
    }

    [Fact]
    public void SendTo()
    {
        Suggest.SendTo(new Feed
        {
            Name = "My App/",
            EntryPoints =
            {
                new EntryPoint {Command = "a"},
                new EntryPoint {Command = "b", SuggestSendTo = true}
            }
        }).Should().Equal(
            new SendTo {Name = "My App- b", Command = "b"});
    }

    [Fact]
    public void Aliases()
    {
        Suggest.Aliases(new Feed
        {
            Name = "My App/",
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun, NeedsTerminal = true},
                new EntryPoint {Command = Command.NameRunGui},
                new EntryPoint {Command = "cli1/", NeedsTerminal = true},
                new EntryPoint {Command = "cli2/", NeedsTerminal = true, BinaryName = "custom"}
            }
        }).Should().Equal(
            new AppAlias {Name = "my-app-", Command = Command.NameRun},
            new AppAlias {Name = "cli1-", Command = "cli1/"},
            new AppAlias {Name = "custom", Command = "cli2/"});
    }

    [Fact]
    public void AliasesFollowNameRules()
    {
        Suggest.Aliases(new Feed
        {
            Name = ".My: App.",
            EntryPoints =
            {
                new EntryPoint {Command = Command.NameRun, NeedsTerminal = true},
                new EntryPoint {Command = "a=b", NeedsTerminal = true},
                new EntryPoint {Command = "reserved", NeedsTerminal = true, BinaryName = "0install"},
                new EntryPoint {Command = "device", NeedsTerminal = true, BinaryName = "nul"}
            }
        }).Should().Equal(
            new AppAlias {Name = "my--app", Command = Command.NameRun},
            new AppAlias {Name = "a-b", Command = "a=b"});
    }

    [Fact]
    public void AutoStart()
    {
        Suggest.AutoStart(new Feed
        {
            Name = "My App/",
            EntryPoints =
            {
                new EntryPoint {Command = "a"},
                new EntryPoint {Command = "b", SuggestAutoStart = true}
            }
        }).Should().Equal(
            new AutoStart {Name = "My App- b", Command = "b"});
    }
}
