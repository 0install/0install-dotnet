// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Contains test methods for <see cref="CommandLine"/>.
/// </summary>
public class CommandLineTest
{
    private static string GetExec(Verb verb)
        => CommandLine.GetRunArguments(new("http://example.com/a.xml"), verb).ToExec("%f");

    [Fact]
    public void ToExecEscaping()
        => new[] {"/opt/my app/0install", "100%", "", "a\"b$c`d\\e", "plain"}.ToExec()
            .Should().Be("""
                         "/opt/my app/0install" 100%% "" "a\"b\$c\`d\\e" plain
                         """);

    [Fact]
    public void ToExecQuotesReservedCharacters()
    {
        foreach (char c in " \t\n\"'\\><~|&;$*?#()`")
            new[] {$"a{c}b"}.ToExec().Should().StartWith("\"").And.EndWith("\"", because: "required by Desktop Entry Specification");
    }

    [Fact]
    public void VerbWithoutArguments()
        => GetExec(new() {Name = Verb.NameOpen}).Should().Be("run http://example.com/a.xml %f");

    [Fact]
    public void VerbWithArguments()
        => GetExec(new() {Name = Verb.NameOpen, Command = "gui", Arguments = {"--open", "${item}", "my arg"}})
           .Should().Be("run --command gui http://example.com/a.xml --open %f \"my arg\"");

    [Fact]
    public void VerbWithEmbeddedItem()
        => GetExec(new() {Name = Verb.NameOpen, Arguments = {"--my file=${item}"}})
           .Should().Be("run http://example.com/a.xml \"--my file=\"%f", because: "field codes must not be inside quotes");

    [Fact]
    public void VerbWithMultipleItems()
        => GetExec(new() {Name = Verb.NameOpen, Arguments = {"--in=${item}", "my arg", "${item}"}})
           .Should().Be("""
                        /bin/sh -c "exec run http://example.com/a.xml --in=\"\$1\" \"my arg\" \"\$1\"" sh %f
                        """, because: "there must not be more than one field code");

    [Theory]
    [InlineData("\"%V\"", "%f")]
    [InlineData("-o \"%1\"", "-o %f")]
    [InlineData("\"%V\" %*", "%f")]
    [InlineData("--file=%V", "--file=%f")]
    public void VerbWithLiteralArguments(string literal, string expected)
        => GetExec(new() {Name = Verb.NameOpen, ArgumentsLiteral = literal})
           .Should().Be($"run http://example.com/a.xml {expected}");
}
