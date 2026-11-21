// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Contains test methods for <see cref="WindowsDeviceNames"/>.
/// </summary>
public class WindowsDeviceNamesTest
{
    [Theory]
    [InlineData("NUL")]
    [InlineData("con")]
    [InlineData("COM1.log")]
    [InlineData("lpt²")]
    [InlineData("AUX .txt")]
    public void Reserved(string fileName)
        => WindowsDeviceNames.IsReserved(fileName).Should().BeTrue();

    [Theory]
    [InlineData("console")]
    [InlineData("auxiliary.c")]
    [InlineData("COM10")]
    [InlineData("my-nul")]
    public void NotReserved(string fileName)
        => WindowsDeviceNames.IsReserved(fileName).Should().BeFalse();
}
