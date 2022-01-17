// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.Capture;

/// <summary>
/// Contains test methods for <see cref="CommandMapper"/>.
/// </summary>
public class CommandMapperTest
{
    /// <summary>
    /// Ensures <see cref="CommandMapper.GetCommand"/> correctly finds the best possible <see cref="Command"/> matches for command-lines;
    /// </summary>
    [Fact]
    public void GetCommand()
    {
        var commandNoArgs = new Command {Name = "no-args", Path = "entry.exe"};
        var commandArgs1 = new Command {Name = "args1", Path = "entry.exe", Arguments = {"--arg1", "long argument"}};
        var commandArgs2 = new Command {Name = "args2", Path = "entry.exe", Arguments = {"--arg2", "long argument"}};
        var provider = new CommandMapper("installation directory", new[] {commandNoArgs, commandArgs1, commandArgs2});


        provider.GetCommand("installation directory" + Path.DirectorySeparatorChar + "entry.exe", out string additionalArgs)
                .Should().BeSameAs(commandNoArgs);
        additionalArgs.Should().Be("");

        provider.GetCommand("\"installation directory" + Path.DirectorySeparatorChar + "entry.exe\" --arg1", out additionalArgs)
                .Should().BeSameAs(commandNoArgs);
        additionalArgs.Should().Be("--arg1");

        provider.GetCommand("\"installation directory" + Path.DirectorySeparatorChar + "entry.exe\" --arg1 \"long argument\" bla", out additionalArgs)
                .Should().BeSameAs(commandArgs1);
        additionalArgs.Should().Be("bla");

        provider.GetCommand("\"installation directory" + Path.DirectorySeparatorChar + "entry.exe\" --arg2 \"long argument\" bla", out additionalArgs)
                .Should().BeSameAs(commandArgs2);
        additionalArgs.Should().Be("bla");

        provider.GetCommand("Something" + Path.DirectorySeparatorChar + "else.exe", out additionalArgs)
                .Should().BeNull();
    }
}
