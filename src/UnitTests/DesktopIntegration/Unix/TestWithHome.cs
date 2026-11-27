// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Prevents other tests from running in parallel with tests derived from <see cref="TestWithHome"/>, which modify process-wide environment variables.
/// </summary>
[CollectionDefinition(nameof(TestWithHomeCollection), DisableParallelization = true)]
public class TestWithHomeCollection;

/// <summary>
/// Base class for tests that redirect the home directory. Only runs on Unix.
/// </summary>
[Collection(nameof(TestWithHomeCollection))]
public abstract class TestWithHome : IDisposable
{
    protected readonly TemporaryDirectory Home;

    private readonly Dictionary<string, string?> _originalVariables =
        new[] {"HOME", "PATH", "SHELL", "XDG_DATA_HOME", "XDG_DATA_DIRS", "XDG_CONFIG_HOME"}
           .ToDictionary(x => x, Environment.GetEnvironmentVariable);

    protected TestWithHome(bool macOS = true)
    {
        Assert.SkipUnless(UnixUtils.IsUnix, "Only used on Unix");
        Assert.SkipWhen(!macOS && UnixUtils.IsMacOSX, "Not used on macOS");

        // The .NET runtime stores named mutexes in a directory below HOME and caches its location on first use.
        // Make sure this happens before we redirect HOME, otherwise all later named mutexes in this process would point to a deleted directory.
        new Mutex(initiallyOwned: false, nameof(TestWithHome)).Dispose();

        Home = new("0install-test-home");
        Environment.SetEnvironmentVariable("HOME", Home);
        Environment.SetEnvironmentVariable("SHELL", "/bin/sh");
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
        Environment.SetEnvironmentVariable("XDG_DATA_DIRS", HomeFile("system"));
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
    }

    public virtual void Dispose()
    {
        foreach (var (key, value) in _originalVariables)
            Environment.SetEnvironmentVariable(key, value);
        Home.Dispose();
    }

    protected string? OriginalPath => _originalVariables["PATH"];

    protected string HomeFile(string name) => Path.Combine(Home, name);
}
