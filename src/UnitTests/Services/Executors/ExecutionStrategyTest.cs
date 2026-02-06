// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Contains test methods for execution strategy classes.
/// </summary>
public class ExecutionStrategyTest : TestWithRedirect
{
    [Fact]
    public void NativeProcessStrategyPathMapper()
    {
        var strategy = new NativeProcessStrategy();
        strategy.PathMapper.MapPath("/some/path").Should().Be("/some/path");
        strategy.PathMapper.UnmapPath("/some/path").Should().Be("/some/path");
    }

    [Fact]
    public void NativeProcessStrategyCreatesContext()
    {
        var strategy = new NativeProcessStrategy();
        var context = strategy.CreateContext();
        context.Should().NotBeNull();
        context.Should().BeOfType<NativeExecutionContext>();
    }

    [Fact]
    public void NativeExecutionContextEnvironmentVariables()
    {
        var context = new NativeExecutionContext();

        context.SetEnvironmentVariable("TEST_VAR", "test_value");
        context.GetEnvironmentVariable("TEST_VAR").Should().Be("test_value");
        context.ContainsEnvironmentVariable("TEST_VAR").Should().BeTrue();
        context.ContainsEnvironmentVariable("NONEXISTENT").Should().BeFalse();
    }

    [Fact]
    public void DockerStrategyPathMapper()
    {
        var strategy = new DockerStrategy("ubuntu:latest");
        string hostPath = "/home/user/cache/implementation";
        string mappedPath = strategy.PathMapper.MapPath(hostPath);

        mappedPath.Should().StartWith("/0install/");
        mappedPath.Should().Contain("implementation");
    }

    [Fact]
    public void WSLStrategyPathMapper()
    {
        var strategy = new WSLStrategy();

        // Windows to WSL
        strategy.PathMapper.MapPath(@"C:\Users\test").Should().Be("/mnt/c/Users/test");
        strategy.PathMapper.MapPath(@"D:\data\file").Should().Be("/mnt/d/data/file");

        // WSL to Windows
        strategy.PathMapper.UnmapPath("/mnt/c/Users/test").Should().Be(@"C:\Users\test");
        strategy.PathMapper.UnmapPath("/mnt/d/data/file").Should().Be(@"D:\data\file");
    }

    [Fact]
    public void WineStrategyPathMapper()
    {
        var strategy = new WineStrategy();

        // Linux to Wine
        strategy.PathMapper.MapPath("/home/user/file").Should().Be(@"Z:\home\user\file");
        strategy.PathMapper.MapPath("/usr/bin/app").Should().Be(@"Z:\usr\bin\app");

        // Wine to Linux
        strategy.PathMapper.UnmapPath(@"Z:\home\user\file").Should().Be("/home/user/file");
    }

    [Fact]
    public void WindowsSandboxStrategyCreatesContext()
    {
        var strategy = new WindowsSandboxStrategy();
        var context = strategy.CreateContext();
        context.Should().NotBeNull();
    }

    [Fact]
    public void ExecutorWithNativeStrategy()
    {
        var storeMock = new Mock<IImplementationStore>(MockBehavior.Loose);
        storeMock.Setup(x => x.GetPath(It.IsAny<ManifestDigest>())).Returns("test path");

        var executor = new Executor(storeMock.Object, new NativeProcessStrategy());
        executor.Should().NotBeNull();
    }

    [Fact]
    public void ExecutorWithDockerStrategy()
    {
        var storeMock = new Mock<IImplementationStore>(MockBehavior.Loose);
        storeMock.Setup(x => x.GetPath(It.IsAny<ManifestDigest>())).Returns("test path");

        var executor = new Executor(storeMock.Object, new DockerStrategy());
        executor.Should().NotBeNull();
    }

    [Fact]
    public void NativeStrategyAppliesEnvironmentBinding()
    {
        var strategy = new NativeProcessStrategy();
        var context = new NativeExecutionContext();
        var implementation = new ImplementationSelection
        {
            InterfaceUri = new("http://example.com/test"),
            ID = "test-id",
            Version = new("1.0")
        };

        var binding = new EnvironmentBinding
        {
            Name = "TEST_PATH",
            Insert = "bin",
            Mode = EnvironmentMode.Prepend
        };

        strategy.ApplyEnvironmentBinding(context, binding, implementation, "/test/path");

        context.GetEnvironmentVariable("TEST_PATH").Should().NotBeNull();
        context.GetEnvironmentVariable("TEST_PATH").Should().Contain("bin");
    }
}
