// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="ZeroInstallDeployment"/>.
/// </summary>
public class ZeroInstallDeploymentTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir = new("0install-test-deployment");

    public void Dispose() => _tempDir.Dispose();

    private void MakeAppBundle()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "Contents", "MacOS"));
        FileUtils.Touch(Path.Combine(_tempDir, "Contents", "Info.plist"));
    }

    [Fact]
    public void FlatDirectory()
    {
        ZeroInstallDeployment.IsAppBundle(_tempDir).Should().BeFalse();
        ZeroInstallDeployment.GetBinaryDir(_tempDir).Should().Be(_tempDir.Path);
    }

    [Fact]
    public void AppBundle()
    {
        MakeAppBundle();

        ZeroInstallDeployment.IsAppBundle(_tempDir).Should().BeTrue();
        ZeroInstallDeployment.UsesAppBundleLayout(_tempDir).Should().Be(UnixUtils.IsMacOSX);
        ZeroInstallDeployment.GetBinaryDir(_tempDir).Should().Be(UnixUtils.IsMacOSX
            ? Path.Combine(_tempDir, "Contents", "MacOS")
            : _tempDir.Path);
    }

    [Fact]
    public void BinaryDirWithKnownLayout()
    {
        ZeroInstallDeployment.GetBinaryDir(_tempDir, appBundle: true).Should().Be(Path.Combine(_tempDir, "Contents", "MacOS"));
        ZeroInstallDeployment.GetBinaryDir(_tempDir, appBundle: false).Should().Be(_tempDir.Path);
    }

    [Fact]
    public void AppBundleWithoutInfoPlist()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "Contents", "MacOS"));
        ZeroInstallDeployment.IsAppBundle(_tempDir).Should().BeFalse();
    }

    [Fact]
    public void NonExistentDirectory()
    {
        string path = Path.Combine(_tempDir, "missing.app");
        ZeroInstallDeployment.IsAppBundle(path).Should().BeFalse();
        ZeroInstallDeployment.GetBinaryDir(path).Should().Be(path);
        ZeroInstallDeployment.FindExecutable(path).Should().BeNull();
    }

    [Fact]
    public void Portable()
    {
        ZeroInstallDeployment.IsPortable(_tempDir).Should().BeFalse();

        ZeroInstallDeployment.SetPortable(_tempDir, true);
        File.Exists(Path.Combine(_tempDir, Locations.PortableFlagName)).Should().BeTrue();
        ZeroInstallDeployment.IsPortable(_tempDir).Should().BeTrue();

        ZeroInstallDeployment.SetPortable(_tempDir, false);
        File.Exists(Path.Combine(_tempDir, Locations.PortableFlagName)).Should().BeFalse();
        ZeroInstallDeployment.IsPortable(_tempDir).Should().BeFalse();
    }

    [Fact]
    public void PortableAppBundle()
    {
        Assert.SkipUnless(UnixUtils.IsMacOSX, "App bundles are only relevant on macOS");

        MakeAppBundle();

        ZeroInstallDeployment.SetPortable(_tempDir, true);
        File.Exists(Path.Combine(_tempDir, "Contents", "MacOS", Locations.PortableFlagName)).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, Locations.PortableFlagName)).Should().BeFalse(because: "App bundles keep the portable flag next to their binaries.");
        ZeroInstallDeployment.IsPortable(_tempDir).Should().BeTrue();

        ZeroInstallDeployment.SetPortable(_tempDir, false);
        ZeroInstallDeployment.IsPortable(_tempDir).Should().BeFalse();
    }

    [Fact]
    public void PortableAppBundleWhileStaging()
    {
        // Simulates staging: directories exist, but Info.plist is not at its final path yet
        Directory.CreateDirectory(Path.Combine(_tempDir, "Contents", "MacOS"));

        ZeroInstallDeployment.SetPortable(_tempDir, true, appBundle: true);
        File.Exists(Path.Combine(_tempDir, "Contents", "MacOS", Locations.PortableFlagName)).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, Locations.PortableFlagName)).Should().BeFalse();

        ZeroInstallDeployment.SetPortable(_tempDir, false, appBundle: true);
        File.Exists(Path.Combine(_tempDir, "Contents", "MacOS", Locations.PortableFlagName)).Should().BeFalse();
    }

    [Fact]
    public void PortableIgnoresAppBundleOutsideMacOS()
    {
        Assert.SkipWhen(UnixUtils.IsMacOSX, "Tests non-macOS platforms");

        MakeAppBundle();
        ZeroInstallDeployment.SetPortable(_tempDir, true);
        File.Exists(Path.Combine(_tempDir, Locations.PortableFlagName)).Should().BeTrue(because: "The portable flag must be next to the executable FindExecutable() returns.");
        ZeroInstallDeployment.IsPortable(_tempDir).Should().BeTrue();
    }

    [Fact]
    public void SetPortableFalseWhenNotPortable()
    {
        ZeroInstallDeployment.SetPortable(_tempDir, false);
        ZeroInstallDeployment.IsPortable(_tempDir).Should().BeFalse();
    }

    [Fact]
    public void IsPortableNonExistentDirectory()
        => ZeroInstallDeployment.IsPortable(Path.Combine(_tempDir, "missing")).Should().BeFalse();

    [Fact]
    public void FindExecutable()
    {
        Assert.SkipUnless(UnixUtils.IsMacOSX, "App bundles are only relevant on macOS");

        FileUtils.Touch(Path.Combine(_tempDir, "0install"));
        ZeroInstallDeployment.FindExecutable(_tempDir).Should().Be(Path.Combine(_tempDir, "0install"));

        MakeAppBundle();
        ZeroInstallDeployment.FindExecutable(_tempDir).Should().BeNull(because: "App bundles keep their binaries in Contents/MacOS.");

        FileUtils.Touch(Path.Combine(_tempDir, "Contents", "MacOS", "0install"));
        ZeroInstallDeployment.FindExecutable(_tempDir).Should().Be(Path.Combine(_tempDir, "Contents", "MacOS", "0install"));
    }

    [Fact]
    public void FindExecutableGui()
    {
        Assert.SkipWhen(WindowsUtils.IsWindows, "Tests Unix executable names");

        FileUtils.Touch(Path.Combine(_tempDir, "0install"));
        ZeroInstallDeployment.FindExecutable(_tempDir, gui: true).Should().BeNull(because: "The command-line executable must not satisfy a GUI lookup.");

        FileUtils.Touch(Path.Combine(_tempDir, "0install-gui"));
        ZeroInstallDeployment.FindExecutable(_tempDir, gui: true).Should().Be(Path.Combine(_tempDir, "0install-gui"));
    }

    [Fact]
    public void FindExecutableGuiAppBundle()
    {
        Assert.SkipUnless(UnixUtils.IsMacOSX, "App bundles are only relevant on macOS");

        MakeAppBundle();
        FileUtils.Touch(Path.Combine(_tempDir, "Contents", "MacOS", "0install-gui"));
        ZeroInstallDeployment.FindExecutable(_tempDir, gui: true).Should().Be(Path.Combine(_tempDir, "Contents", "MacOS", "0install-gui"));
    }

    [Fact]
    public void FindExecutableWindows()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Tests Windows executable names");

        ZeroInstallDeployment.FindExecutable(_tempDir).Should().BeNull();
        ZeroInstallDeployment.FindExecutable(_tempDir, gui: true).Should().BeNull();

        FileUtils.Touch(Path.Combine(_tempDir, "0install.exe"));
        ZeroInstallDeployment.FindExecutable(_tempDir).Should().Be(Path.Combine(_tempDir, "0install.exe"));
        ZeroInstallDeployment.FindExecutable(_tempDir, gui: true).Should().BeNull(because: "The command-line executable must not satisfy a GUI lookup.");

        FileUtils.Touch(Path.Combine(_tempDir, "0install-win.exe"));
        ZeroInstallDeployment.FindExecutable(_tempDir, gui: true).Should().Be(Path.Combine(_tempDir, "0install-win.exe"));
    }

    [Theory, InlineData(false), InlineData(true)]
    public void GetDefaultPath(bool machineWide)
    {
        string path = ZeroInstallDeployment.GetDefaultPath(machineWide);
        Path.IsPathRooted(path).Should().BeTrue();
        path.StartsWith(Locations.HomeDir).Should().Be(!machineWide);

        string appBundlePath = ZeroInstallDeployment.GetDefaultPath(machineWide, appBundle: true);
        if (UnixUtils.IsMacOSX) appBundlePath.Should().EndWith(".app");
        else appBundlePath.Should().Be(path, because: "App bundles only exist on macOS.");
    }

    [Fact]
    public void FindExecutableWindowsIgnoresAppBundle()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "Tests Windows executable lookup");

        MakeAppBundle();
        FileUtils.Touch(Path.Combine(_tempDir, "Contents", "MacOS", "0install.exe"));
        ZeroInstallDeployment.FindExecutable(_tempDir).Should().BeNull(because: "Windows deployments keep their binaries in the root directory.");
    }
}
