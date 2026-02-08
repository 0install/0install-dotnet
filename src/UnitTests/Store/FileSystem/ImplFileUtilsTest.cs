// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.FileSystem;

public class ImplFileUtilsTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir = new("0install-unit-test-implfileutils");

    public void Dispose() => _tempDir.Dispose();

    [SkippableFact]
    public void SetExecutableWritesBothAttributes()
    {
        Skip.IfNot(WindowsUtils.IsWindowsNT, "This test only runs on Windows NT.");

        string testFile = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(testFile, "test content");

        ImplFileUtils.SetExecutable(testFile);

        // Verify xbit is set
        FileUtils.ReadExtendedMetadata(testFile, "xbit").Should().NotBeNull("xbit should be set for backwards compatibility");

        // Verify $LXMOD is set
        var lxmodData = FileUtils.ReadExtendedMetadata(testFile, "$LXMOD");
        lxmodData.Should().NotBeNull("$LXMOD should be set for WSL compatibility");
        lxmodData!.Length.Should().BeGreaterOrEqualTo(4, "$LXMOD should contain at least 4 bytes for mode");

        // Verify the mode contains executable bit
        int mode = BitConverter.ToInt32(lxmodData, 0);
        (mode & 0x40).Should().NotBe(0, "owner execute bit should be set in $LXMOD");
    }

    [SkippableFact]
    public void IsExecutableDetectsXbit()
    {
        Skip.IfNot(WindowsUtils.IsWindowsNT, "This test only runs on Windows NT.");

        string testFile = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(testFile, "test content");

        // Set only xbit (simulating old behavior)
        FileUtils.WriteExtendedMetadata(testFile, "xbit", []);

        ImplFileUtils.IsExecutable(testFile).Should().BeTrue("file with xbit should be detected as executable");
    }

    [SkippableFact]
    public void IsExecutableDetectsLxMod()
    {
        Skip.IfNot(WindowsUtils.IsWindowsNT, "This test only runs on Windows NT.");

        string testFile = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(testFile, "test content");

        // Set only $LXMOD with executable bit (0755 octal = 493 decimal)
        byte[] modeBytes = BitConverter.GetBytes(493); // 0755 in octal
        FileUtils.WriteExtendedMetadata(testFile, "$LXMOD", modeBytes);

        ImplFileUtils.IsExecutable(testFile).Should().BeTrue("file with $LXMOD executable bit should be detected as executable");
    }

    [SkippableFact]
    public void IsExecutableIgnoresNonExecutableLxMod()
    {
        Skip.IfNot(WindowsUtils.IsWindowsNT, "This test only runs on Windows NT.");

        string testFile = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(testFile, "test content");

        // Set $LXMOD without executable bit (0644 octal = 420 decimal - rw-r--r--)
        byte[] modeBytes = BitConverter.GetBytes(420); // 0644 in octal
        FileUtils.WriteExtendedMetadata(testFile, "$LXMOD", modeBytes);

        ImplFileUtils.IsExecutable(testFile).Should().BeFalse("file with $LXMOD without executable bit should not be detected as executable");
    }

    [Fact]
    public void IsExecutableRoundTrip()
    {
        string testFile = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(testFile, "test content");

        ImplFileUtils.IsExecutable(testFile).Should().BeFalse("new file should not be executable");

        ImplFileUtils.SetExecutable(testFile);

        ImplFileUtils.IsExecutable(testFile).Should().BeTrue("file should be executable after SetExecutable");
    }

    [SkippableFact]
    public void SetExecutablePreservesExistingPermissions()
    {
        Skip.IfNot(WindowsUtils.IsWindowsNT, "This test only runs on Windows NT.");

        string testFile = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(testFile, "test content");

        // Set $LXMOD with specific permissions (0666 = rw-rw-rw- without execute bit)
        int initialMode = 438; // 0666 in octal
        byte[] initialModeBytes = BitConverter.GetBytes(initialMode);
        FileUtils.WriteExtendedMetadata(testFile, "$LXMOD", initialModeBytes);

        // Set executable bit
        ImplFileUtils.SetExecutable(testFile);

        // Verify $LXMOD now has executable bit set but other bits preserved
        var lxmodData = FileUtils.ReadExtendedMetadata(testFile, "$LXMOD");
        lxmodData.Should().NotBeNull("$LXMOD should still be set");
        int finalMode = BitConverter.ToInt32(lxmodData!, 0);
        
        // Check that executable bit is now set
        (finalMode & 0x40).Should().NotBe(0, "owner execute bit should be set");
        
        // Check that other bits are preserved (0666 | 0100 = 0766)
        finalMode.Should().Be(502, "mode should be 0766 (0666 with owner execute bit added)"); // 502 = 0766 octal
    }
}
